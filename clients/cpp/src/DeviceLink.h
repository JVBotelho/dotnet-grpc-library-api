#pragma once
#include "kiosk.grpc.pb.h"
#include <thread>
#include <mutex>
#include <condition_variable>
#include <queue>
#include <atomic>
#include <functional>
#include <memory>

enum class QueuePolicy { DropOldest, Block };

class DeviceLink {
public:
    DeviceLink(LibrarySystem::Contracts::Protos::Kiosk::Stub* stub, int queue_capacity, QueuePolicy queue_policy, const std::string& api_key);
    ~DeviceLink();

    void Start();
    void Stop();
    void EnqueueFrame(const LibrarySystem::Contracts::Protos::DeviceFrame& frame);
    void SetControlCommandCallback(std::function<void(const LibrarySystem::Contracts::Protos::ControlCommand&)> callback);
    bool IsOnline() const { return is_online_; }

private:
    LibrarySystem::Contracts::Protos::Kiosk::Stub* stub_;
    int queue_capacity_;
    QueuePolicy queue_policy_;
    std::string api_key_;

    std::atomic<bool> running_{false};
    std::atomic<bool> is_online_{true};
    std::thread worker_thread_;

    std::mutex queue_mutex_;
    std::condition_variable queue_cv_;
    std::queue<LibrarySystem::Contracts::Protos::DeviceFrame> frame_queue_;

    std::function<void(const LibrarySystem::Contracts::Protos::ControlCommand&)> control_callback_;
};
