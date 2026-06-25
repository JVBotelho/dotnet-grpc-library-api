#include "DeviceLink.h"
#include <iostream>

using grpc::ClientContext;
using grpc::Status;
using LibrarySystem::Contracts::Protos::DeviceFrame;
using LibrarySystem::Contracts::Protos::ControlCommand;

DeviceLink::DeviceLink(LibrarySystem::Contracts::Protos::Kiosk::Stub* stub, int queue_capacity, QueuePolicy queue_policy, const std::string& api_key)
    : stub_(stub), queue_capacity_(queue_capacity), queue_policy_(queue_policy), api_key_(api_key) {}

DeviceLink::~DeviceLink() {
    Stop();
}

void DeviceLink::SetControlCommandCallback(std::function<void(const ControlCommand&)> callback) {
    control_callback_ = callback;
}

void DeviceLink::Start() {
    if (running_) return;
    running_ = true;
    is_online_ = true;

    worker_thread_ = std::thread([this]() {
        ClientContext context;
        if (!api_key_.empty()) context.AddMetadata("x-api-key", api_key_);
        auto stream = stub_->DeviceLink(&context);

        std::thread reader([this, stream = stream.get()]() {
            ControlCommand cmd;
            while (stream->Read(&cmd)) {
                if (control_callback_) {
                    control_callback_(cmd);
                }
            }
        });

        while (running_) {
            DeviceFrame frame;
            {
                std::unique_lock<std::mutex> lock(queue_mutex_);
                queue_cv_.wait_for(lock, std::chrono::milliseconds(100), [this] { 
                    return !frame_queue_.empty() || !running_; 
                });

                if (!running_) break;
                if (frame_queue_.empty()) continue;

                frame = frame_queue_.front();
                frame_queue_.pop();
            }

            if (!stream->Write(frame)) {
                std::cerr << "[DeviceLink] Stream write failed (server disconnected)" << std::endl;
                is_online_ = false;
                break;
            }
        }

        stream->WritesDone();
        reader.join();
        Status status = stream->Finish();

        if (!status.ok()) {
            std::cerr << "[DeviceLink] Finished with Error " << status.error_code() << ": " << status.error_message() << std::endl;
        } else {
            std::cout << "[DeviceLink] Closed successfully." << std::endl;
        }

        running_ = false;
        is_online_ = false;
    });
}

void DeviceLink::Stop() {
    running_ = false;
    queue_cv_.notify_all();
    if (worker_thread_.joinable()) {
        worker_thread_.join();
    }
}

void DeviceLink::EnqueueFrame(const DeviceFrame& frame) {
    std::unique_lock<std::mutex> lock(queue_mutex_);
    
    if (frame_queue_.size() >= queue_capacity_) {
        if (queue_policy_ == QueuePolicy::DropOldest) {
            frame_queue_.pop(); // Drop oldest to make room
        } else {
            // Block until space is available or stopped
            queue_cv_.wait(lock, [this] { return frame_queue_.size() < queue_capacity_ || !running_; });
            if (!running_) return;
        }
    }
    
    frame_queue_.push(frame);
    queue_cv_.notify_one();
}
