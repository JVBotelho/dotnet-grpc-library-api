#pragma once
#include <string>
#include <functional>
#include <thread>
#include <atomic>
#include "kiosk.pb.h" // For DeviceFrame

class CanReader {
public:
    using FrameCallback = std::function<void(const LibrarySystem::Contracts::Protos::DeviceFrame&)>;

    explicit CanReader(const std::string& interface_name, const std::string& device_id);
    ~CanReader();

    bool Start(FrameCallback callback);
    void Stop();

private:
    void RunLoop();
    LibrarySystem::Contracts::Protos::DeviceFrame ParseFrame(uint32_t can_id, const uint8_t* data, uint8_t len);

    std::string interface_name_;
    std::string device_id_;
    int socket_fd_;
    std::atomic<bool> running_;
    std::thread thread_;
    FrameCallback callback_;
};
