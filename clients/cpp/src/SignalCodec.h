#pragma once
#include <cstdint>
#include <string>
#include "kiosk.pb.h"

enum class Endianness {
    LittleEndian, // Intel
    BigEndian     // Motorola
};

struct SignalDefinition {
    std::string name;
    uint32_t start_bit;
    uint32_t length;
    bool is_signed;
    Endianness endianness;
    double scale;
    double offset;
};

class SignalCodec {
public:
    static LibrarySystem::Contracts::Protos::DeviceFrame DecodeFrame(uint32_t can_id, const uint8_t* data, uint8_t len, const std::string& device_id);

private:
    static double ExtractSignal(const uint8_t* data, uint8_t len, const SignalDefinition& sig);
    static uint64_t ExtractRawBits(const uint8_t* data, uint8_t len, uint32_t start_bit, uint32_t length, Endianness endianness);
};
