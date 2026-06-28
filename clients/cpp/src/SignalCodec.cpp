#include "SignalCodec.h"
#include <chrono>
#include <vector>
#include <cmath>
#include <iostream>

// Kiosk Telemetry Signal Database
static const std::vector<SignalDefinition> KIOSK_SIGNALS = {
    {"BeltMotorTemp", 0,  8,  false, Endianness::LittleEndian, 0.5, 20.0},
    {"ScannerRPM",    8,  12, false, Endianness::LittleEndian, 10.0, 0.0},
    {"SafetyDoor",    20, 1,  false, Endianness::LittleEndian, 1.0, 0.0},
    {"BayOccupancy",  21, 3,  false, Endianness::LittleEndian, 1.0, 0.0},
    {"FaultFlags",    39, 32, false, Endianness::BigEndian,    1.0, 0.0}
};

uint64_t SignalCodec::ExtractRawBits(const uint8_t* data, uint8_t len, uint32_t start_bit, uint32_t length, Endianness endianness) {
    if (length == 0 || length > 64) return 0;
    
    uint64_t raw_val = 0;
    
    if (endianness == Endianness::LittleEndian) {
        // Intel format: start_bit is the LSB of the signal.
        uint32_t current_bit = start_bit;
        for (uint32_t i = 0; i < length; ++i) {
            uint32_t byte_idx = current_bit / 8;
            uint32_t bit_idx = current_bit % 8;
            if (byte_idx < len) {
                uint8_t bit_val = (data[byte_idx] >> bit_idx) & 0x01;
                raw_val |= (static_cast<uint64_t>(bit_val) << i);
            }
            current_bit++;
        }
    } else {
        // Motorola format: start_bit is the MSB of the signal.
        // We read from MSB down to LSB.
        uint32_t current_bit = start_bit;
        for (uint32_t i = 0; i < length; ++i) {
            uint32_t byte_idx = current_bit / 8;
            uint32_t bit_idx = current_bit % 8;
            if (byte_idx < len) {
                uint8_t bit_val = (data[byte_idx] >> bit_idx) & 0x01;
                raw_val |= (static_cast<uint64_t>(bit_val) << (length - 1 - i));
            }
            // In Motorola backward, the next less significant bit is at current_bit - 1
            // Wrapping logic across bytes: bit 0 of byte N is followed by bit 7 of byte N+1
            if (bit_idx == 0) {
                current_bit += 15;
            } else {
                current_bit -= 1;
            }
        }
    }
    
    return raw_val;
}

double SignalCodec::ExtractSignal(const uint8_t* data, uint8_t len, const SignalDefinition& sig) {
    uint64_t raw = ExtractRawBits(data, len, sig.start_bit, sig.length, sig.endianness);
    
    int64_t signed_val = 0;
    if (sig.is_signed) {
        // Sign extension
        if (raw & (1ULL << (sig.length - 1))) {
            uint64_t mask = ~0ULL << sig.length;
            raw |= mask;
        }
        signed_val = static_cast<int64_t>(raw);
        return (signed_val * sig.scale) + sig.offset;
    } else {
        return (raw * sig.scale) + sig.offset;
    }
}

LibrarySystem::Contracts::Protos::DeviceFrame SignalCodec::DecodeFrame(uint32_t can_id, const uint8_t* data, uint8_t len, const std::string& device_id) {
    LibrarySystem::Contracts::Protos::DeviceFrame frame;
    frame.set_device_id(device_id);
    
    auto now = std::chrono::system_clock::now();
    auto seconds = std::chrono::time_point_cast<std::chrono::seconds>(now);
    auto nanos = std::chrono::duration_cast<std::chrono::nanoseconds>(now - seconds);
    frame.mutable_sampled_at()->set_seconds(seconds.time_since_epoch().count());
    frame.mutable_sampled_at()->set_nanos(nanos.count());
    
    frame.set_can_id(can_id);

    if (len > 0) {
        frame.set_belt_motor_temp_c(ExtractSignal(data, len, KIOSK_SIGNALS[0]));
        frame.set_scanner_rpm(ExtractSignal(data, len, KIOSK_SIGNALS[1]));
        frame.set_safety_door_closed(ExtractSignal(data, len, KIOSK_SIGNALS[2]) != 0);
        frame.set_bay_occupancy(static_cast<uint32_t>(ExtractSignal(data, len, KIOSK_SIGNALS[3])));
        frame.set_fault_flags(static_cast<uint32_t>(ExtractSignal(data, len, KIOSK_SIGNALS[4])));
    }
    
    return frame;
}
