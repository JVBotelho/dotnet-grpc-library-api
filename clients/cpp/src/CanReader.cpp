#include "CanReader.h"
#include "SignalCodec.h"
#include <iostream>
#include <cstring>
#include <chrono>

#ifdef __linux__
#include <unistd.h>
#include <net/if.h>
#include <sys/ioctl.h>
#include <sys/socket.h>
#include <linux/can.h>
#include <linux/can/raw.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#else
// Mock for compilation on Windows (WSL2/Docker should be used for real CAN)
struct canfd_frame {
    uint32_t can_id;
    uint8_t len;
    uint8_t flags;
    uint8_t data[64];
};
#define PF_CAN 29
#define SOCK_RAW 3
#define CAN_RAW 1
#define CAN_RAW_FD_FRAMES 5
#define CAN_MTU 16
#define CANFD_MTU 72
#define SIOCGIFINDEX 0x8933
#define SOL_CAN_RAW 101
struct sockaddr_can {
    uint16_t can_family;
    int can_ifindex;
    union {
        uint32_t rx;
        uint32_t tx;
    } can_addr;
};
struct ifreq {
    char ifr_name[16];
    int ifr_ifindex;
};
int socket(int domain, int type, int protocol) { return -1; }
int ioctl(int fd, unsigned long request, ...) { return -1; }
int bind(int sockfd, const struct sockaddr *addr, unsigned int addrlen) { return -1; }
int setsockopt(int sockfd, int level, int optname, const void *optval, unsigned int optlen) { return -1; }
int read(int fd, void *buf, unsigned int count) { return -1; }
int close(int fd) { return -1; }
#define AF_INET 2
#define SOCK_DGRAM 2
#define INADDR_ANY 0
uint16_t htons(uint16_t hostshort) { return hostshort; }
struct sockaddr_in {
    short sin_family;
    uint16_t sin_port;
    struct { uint32_t s_addr; } sin_addr;
    char sin_zero[8];
};
#endif

CanReader::CanReader(const std::string& interface_name, const std::string& device_id)
    : interface_name_(interface_name), device_id_(device_id), socket_fd_(-1), running_(false) {
}

CanReader::~CanReader() {
    Stop();
}

bool CanReader::Start(FrameCallback callback) {
    if (running_) return false;

    bool is_udp = (interface_name_.substr(0, 4) == "udp:");
    
    if (is_udp) {
        int port = 0;
        try {
            port = std::stoi(interface_name_.substr(4));
        } catch (const std::exception&) {
            std::cerr << "[CanReader] Invalid UDP port in '" << interface_name_ << "'" << std::endl;
            return false;
        }
        if (port < 1 || port > 65535) {
            std::cerr << "[CanReader] UDP port out of range: " << port << std::endl;
            return false;
        }
        socket_fd_ = socket(AF_INET, SOCK_DGRAM, 0);
        if (socket_fd_ < 0) {
            std::cerr << "[CanReader] Failed to open UDP socket" << std::endl;
            return false;
        }
        
        struct sockaddr_in servaddr;
        std::memset(&servaddr, 0, sizeof(servaddr));
        servaddr.sin_family = AF_INET;
        servaddr.sin_addr.s_addr = INADDR_ANY;
        servaddr.sin_port = htons(port);
        
        if (bind(socket_fd_, (const struct sockaddr *)&servaddr, sizeof(servaddr)) < 0) {
            std::cerr << "[CanReader] Failed to bind UDP socket on port " << port << std::endl;
            close(socket_fd_);
            return false;
        }
        std::cout << "[CanReader] Started listening on UDP port " << port << " (CAN Emulator Mode)" << std::endl;
    } else {
        socket_fd_ = socket(PF_CAN, SOCK_RAW, CAN_RAW);
        if (socket_fd_ < 0) {
            std::cerr << "[CanReader] Failed to open CAN socket. Ensure you run this on Linux." << std::endl;
            return false;
        }

        int enable_canfd = 1;
        if (setsockopt(socket_fd_, SOL_CAN_RAW, CAN_RAW_FD_FRAMES, &enable_canfd, sizeof(enable_canfd)) != 0) {
            std::cerr << "[CanReader] Failed to enable CAN-FD. Make sure MTU is 72." << std::endl;
        }

        struct ifreq ifr;
        std::strncpy(ifr.ifr_name, interface_name_.c_str(), sizeof(ifr.ifr_name) - 1);
        ifr.ifr_name[sizeof(ifr.ifr_name) - 1] = '\0';
        
        if (ioctl(socket_fd_, SIOCGIFINDEX, &ifr) < 0) {
            std::cerr << "[CanReader] Failed to find interface " << interface_name_ << std::endl;
            close(socket_fd_);
            return false;
        }

        struct sockaddr_can addr;
        addr.can_family = PF_CAN;
        addr.can_ifindex = ifr.ifr_ifindex;

        if (bind(socket_fd_, (struct sockaddr *)&addr, sizeof(addr)) < 0) {
            std::cerr << "[CanReader] Failed to bind CAN socket" << std::endl;
            close(socket_fd_);
            return false;
        }
        std::cout << "[CanReader] Started listening on CAN interface " << interface_name_ << std::endl;
    }

    callback_ = callback;
    running_ = true;
    thread_ = std::thread(&CanReader::RunLoop, this);
    
    return true;
}

void CanReader::Stop() {
    running_ = false;
    if (socket_fd_ >= 0) {
        close(socket_fd_);
        socket_fd_ = -1;
    }
    if (thread_.joinable()) {
        thread_.join();
    }
}

void CanReader::RunLoop() {
    struct canfd_frame frame;

    while (running_) {
        std::memset(&frame, 0, sizeof(frame));
        int nbytes = read(socket_fd_, &frame, sizeof(struct canfd_frame));
        if (nbytes < 0) {
            if (running_) {
                std::cerr << "[CanReader] Read error" << std::endl;
            }
            break;
        }

        if (nbytes == CANFD_MTU || nbytes == CAN_MTU) {
            auto pb_frame = ParseFrame(frame.can_id, frame.data, frame.len);
            if (callback_) {
                callback_(pb_frame);
            }
        }
    }
}

LibrarySystem::Contracts::Protos::DeviceFrame CanReader::ParseFrame(uint32_t can_id, const uint8_t* data, uint8_t len) {
    return SignalCodec::DecodeFrame(can_id, data, len, device_id_);
}
