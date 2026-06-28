#include "KioskClient.h"
#include <iostream>
#include <chrono>

using grpc::Channel;
using grpc::ClientContext;
using grpc::Status;
using LibrarySystem::Contracts::Protos::ValidateMemberRequest;
using LibrarySystem::Contracts::Protos::ValidateMemberResponse;

#include <fstream>
#include <sstream>
#include <charconv>

std::string ReadFileContents(const std::string& path) {
    std::ifstream file(path);
    if (!file.is_open()) {
        std::cerr << "Failed to open " << path << std::endl;
        return "";
    }
    std::ostringstream ss;
    ss << file.rdbuf();
    return ss.str();
}

KioskClient::KioskClient(const KioskConfig& config)
    : device_id_(config.device_id), api_key_(config.api_key) {
    
    std::shared_ptr<grpc::ChannelCredentials> creds;
    if (config.use_tls) {
        grpc::SslCredentialsOptions ssl_opts;
        ssl_opts.pem_root_certs = ReadFileContents(config.root_certs_path);
        ssl_opts.pem_cert_chain = ReadFileContents(config.cert_chain_path);
        ssl_opts.pem_private_key = ReadFileContents(config.private_key_path);
        creds = grpc::SslCredentials(ssl_opts);
    } else {
#ifndef NDEBUG
        std::cerr << "[KioskClient] WARNING: TLS disabled. Only valid in local unit-test mode.\n";
#else
        std::cerr << "[KioskClient] FATAL: TLS is required in release builds.\n";
        std::exit(1);
#endif
        creds = grpc::InsecureChannelCredentials();
    }

    offline_store_ = std::make_unique<OfflineStore>("kiosk_offline.db");

    std::shared_ptr<Channel> channel = grpc::CreateChannel(config.server_endpoint, creds);
    channel_ = channel;
    stub_ = LibrarySystem::Contracts::Protos::Kiosk::NewStub(channel);
    
    // Initialize DeviceLink with a queue capacity of 10000 and DropOldest policy
    // Backpressure policy: DropOldest. Real-time telemetry prefers freshness over completeness.
    device_link_ = std::make_unique<DeviceLink>(stub_.get(), 10000, QueuePolicy::DropOldest, api_key_);
    device_link_->SetControlCommandCallback([this](const LibrarySystem::Contracts::Protos::ControlCommand& cmd) {
        this->ProcessControlCommand(cmd);
    });
}

KioskClient::~KioskClient() {
    StopDeviceLink();
    sync_running_ = false;
    if (watcher_thread_.joinable()) watcher_thread_.join();
    if (sync_thread_.joinable()) sync_thread_.join();
}

bool KioskClient::ValidateMember(const std::string& card_uid) {
    ValidateMemberRequest request;
    request.set_device_id(device_id_);
    request.set_card_uid(card_uid);

    ValidateMemberResponse reply;
    ClientContext context;
    context.set_deadline(std::chrono::system_clock::now() + std::chrono::seconds(5));
    if (!api_key_.empty()) context.AddMetadata("x-api-key", api_key_);

    std::cout << "[Client] Validating card " << card_uid << "..." << std::endl;
    Status status = stub_->ValidateMember(&context, request, &reply);

    if (status.ok()) {
        std::cout << "[Server Response] Valid: " << (reply.valid() ? "Yes" : "No") << std::endl;
        if (reply.valid()) {
            std::cout << "  Borrower ID: " << reply.borrower_id() << std::endl;
            std::cout << "  Name: " << reply.display_name() << std::endl;
        } else {
            std::cout << "  Reason: " << reply.reason() << std::endl;
        }
        return reply.valid();
    } else {
        std::cerr << "[RPC Error] " << status.error_code() << ": " << status.error_message() << std::endl;
        return false;
    }
}

void KioskClient::BulkReturn(const std::string& book_id) {
    if (!is_online_) {
        // Enqueue for later
        LibrarySystem::Contracts::Protos::BufferedEvent event;
        event.set_idempotency_key(device_id_ + "_" + book_id + "_" + std::to_string(std::chrono::system_clock::now().time_since_epoch().count()));
        
        auto* scan = event.mutable_return_scan();
        scan->set_device_id(device_id_);
        
        int book_id_int = 0;
        auto [ptr, ec] = std::from_chars(book_id.data(), book_id.data() + book_id.size(), book_id_int);
        if (ec != std::errc{}) {
            std::cerr << "[KioskClient] Invalid book_id: " << book_id << std::endl;
            return;
        }
        scan->set_book_id(book_id_int);
        scan->set_idempotency_key(event.idempotency_key());
        
        auto now = std::chrono::system_clock::now();
        auto seconds = std::chrono::time_point_cast<std::chrono::seconds>(now);
        auto nanos = std::chrono::duration_cast<std::chrono::nanoseconds>(now - seconds);
        scan->mutable_scanned_at()->set_seconds(seconds.time_since_epoch().count());
        scan->mutable_scanned_at()->set_nanos(nanos.count());

        offline_store_->StoreEvent(event);
        std::cout << "[Client] Offline. Buffered return for book " << book_id << std::endl;
        return;
    }

    ClientContext context;
    context.set_deadline(std::chrono::system_clock::now() + std::chrono::seconds(5));
    if (!api_key_.empty()) context.AddMetadata("x-api-key", api_key_);
    
    LibrarySystem::Contracts::Protos::BulkReturnSummary summary;
    auto stream = stub_->BulkReturn(&context, &summary);

    LibrarySystem::Contracts::Protos::ReturnScan scan;
    scan.set_device_id(device_id_);
    
    int book_id_int = 0;
    auto [ptr, ec] = std::from_chars(book_id.data(), book_id.data() + book_id.size(), book_id_int);
    if (ec != std::errc{}) {
        std::cerr << "[KioskClient] Invalid book_id: " << book_id << std::endl;
        return;
    }
    scan.set_book_id(book_id_int);
    scan.set_idempotency_key(device_id_ + "_" + book_id + "_" + std::to_string(std::chrono::system_clock::now().time_since_epoch().count()));
    
    auto now = std::chrono::system_clock::now();
    auto seconds = std::chrono::time_point_cast<std::chrono::seconds>(now);
    auto nanos = std::chrono::duration_cast<std::chrono::nanoseconds>(now - seconds);
    scan.mutable_scanned_at()->set_seconds(seconds.time_since_epoch().count());
    scan.mutable_scanned_at()->set_nanos(nanos.count());

    if (!stream->Write(scan)) {
        std::cerr << "[BulkReturn] Stream write failed. Network disconnected?" << std::endl;
        is_online_ = false;
        // fallback to offline store
        LibrarySystem::Contracts::Protos::BufferedEvent event;
        event.set_idempotency_key(scan.idempotency_key());
        *event.mutable_return_scan() = scan;
        offline_store_->StoreEvent(event);
        return;
    }

    stream->WritesDone();
    Status status = stream->Finish();

    if (status.ok()) {
        std::cout << "[BulkReturn] Summary: Accepted=" << summary.accepted() 
                  << " Rejected=" << summary.rejected() << std::endl;
    } else {
        std::cerr << "[BulkReturn] RPC Error " << status.error_code() << ": " << status.error_message() << std::endl;
        is_online_ = false;
    }
}

void KioskClient::EnqueueFrame(const LibrarySystem::Contracts::Protos::DeviceFrame& frame) {
    if (is_online_ && device_link_->IsOnline()) {
        device_link_->EnqueueFrame(frame);
    } else {
        LibrarySystem::Contracts::Protos::BufferedEvent event;
        event.set_idempotency_key(device_id_ + "_" + std::to_string(frame.sampled_at().seconds()) + "_" + std::to_string(frame.sampled_at().nanos()));
        *event.mutable_frame() = frame;
        offline_store_->StoreEvent(event);
    }
}

void KioskClient::StopDeviceLink() {
    device_link_->Stop();
}

void KioskClient::ProcessControlCommand(const LibrarySystem::Contracts::Protos::ControlCommand& cmd) {
    std::cout << "[DeviceLink] Received ControlCommand: " 
              << "Kind=" << cmd.kind() 
              << ", Reason='" << cmd.reason() << "'"
              << ", Arg=" << cmd.arg() << std::endl;
}

// DeviceLink is a long-lived bidi stream. No deadline is set; the connection watcher
// (ConnectionWatcherLoop) detects channel failure and sets is_online_ = false, which
// causes RunDeviceLink to exit and allows ConnectAndSync to restart it on recovery.
void KioskClient::RunDeviceLink() {
    device_link_->Start();
}

void KioskClient::ConnectAndSync() {
    if (sync_running_) return;
    sync_running_ = true;

    watcher_thread_ = std::thread(&KioskClient::ConnectionWatcherLoop, this);
    sync_thread_ = std::thread(&KioskClient::SyncQueueLoop, this);
}

void KioskClient::ConnectionWatcherLoop() {
    while (sync_running_) {
        auto channel = channel_;
        auto state = channel->GetState(true); 
        
        bool currently_online = (state == grpc_connectivity_state::GRPC_CHANNEL_READY);
        if (currently_online && !is_online_) {
            std::cout << "[Watcher] Network ONLINE!" << std::endl;
            is_online_ = true;
            // Optionally, automatically start the DeviceLink if not running
            if (!link_running_) {
                RunDeviceLink();
            }
        } else if (!currently_online && is_online_) {
            std::cout << "[Watcher] Network OFFLINE. Fallback to Store-And-Forward." << std::endl;
            is_online_ = false;
        }

        auto deadline = std::chrono::system_clock::now() + std::chrono::seconds(5);
        channel->WaitForStateChange(state, deadline);
    }
}

void KioskClient::SyncQueueLoop() {
    while (sync_running_) {
        std::this_thread::sleep_for(std::chrono::seconds(2));

        if (!is_online_) continue;

        offline_store_->SweepExpired();
        
        auto pending = offline_store_->GetPendingEvents(100);
        if (pending.empty()) continue;

        std::cout << "[Sync] Attempting to sync " << pending.size() << " offline events..." << std::endl;

        ClientContext context;
        context.set_deadline(std::chrono::system_clock::now() + std::chrono::seconds(10));
        if (!api_key_.empty()) context.AddMetadata("x-api-key", api_key_);

        LibrarySystem::Contracts::Protos::SyncSummary summary;
        auto stream = stub_->SyncOfflineQueue(&context, &summary);

        std::vector<int64_t> synced_ids;
        for (const auto& item : pending) {
            if (stream->Write(item.event)) {
                synced_ids.push_back(item.id);
            } else {
                break;
            }
        }

        stream->WritesDone();
        Status status = stream->Finish();

        if (status.ok() && !synced_ids.empty()) {
            std::cout << "[Sync] Successful. Applied: " << summary.applied() 
                      << " Skipped Duplicates: " << summary.duplicates_skipped() << std::endl;
            offline_store_->MarkAsSynced(synced_ids);
        } else {
            std::cerr << "[Sync] Failed to sync. Error: " << status.error_message() << std::endl;
            is_online_ = false; 
        }
    }
}
