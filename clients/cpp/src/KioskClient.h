#pragma once
#include <memory>
#include <string>
#include <thread>
#include <atomic>
#include <mutex>
#include <queue>
#include <condition_variable>
#include <optional>
#include <grpcpp/grpcpp.h>
#include "kiosk.grpc.pb.h"
#include "config.h"
#include "OfflineStore.h"
#include "DeviceLink.h"

class KioskClient {
public:
    explicit KioskClient(const KioskConfig& config);
    ~KioskClient();

    // Phase 2: Unary call
    [[nodiscard]] bool ValidateMember(const std::string& card_uid);

    // Phase 4: Bulk Return client streaming
    void BulkReturn(const std::string& book_id);

    // Phase 3: Bidi DeviceLink
    void RunDeviceLink();
    void EnqueueFrame(const LibrarySystem::Contracts::Protos::DeviceFrame& frame);
    void StopDeviceLink();

    // Phase 4: Offline Store and Forward
    void ConnectAndSync();

private:
    void ProcessControlCommand(const LibrarySystem::Contracts::Protos::ControlCommand& cmd);
    void ConnectionWatcherLoop();
    void SyncQueueLoop();

    std::string device_id_;
    std::shared_ptr<grpc::Channel> channel_;
    std::unique_ptr<LibrarySystem::Contracts::Protos::Kiosk::Stub> stub_;
    std::unique_ptr<OfflineStore> offline_store_;
    std::unique_ptr<DeviceLink> device_link_;
    
    std::atomic<bool> is_online_{false};
    std::atomic<bool> sync_running_{false};
    std::atomic<bool> link_running_{false};
    std::thread watcher_thread_;
    std::thread sync_thread_;
};
