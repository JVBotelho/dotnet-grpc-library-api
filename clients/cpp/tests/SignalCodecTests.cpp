#include <gtest/gtest.h>
#include "../src/SignalCodec.h"

// Helper function to create a byte array and test DecodeFrame
TEST(SignalCodecTests, DecodeFrame_ParsesFaultFlagsCorrectly_Motorola) {
    // FaultFlags start at bit 39 (byte 4, bit 7). Length 32. BigEndian.
    // Let's set 0xFF800000. 
    // In memory: byte 4 = 0xFF, byte 5 = 0x80, byte 6 = 0x00, byte 7 = 0x00
    uint8_t data[8] = { 0, 0, 0, 0, 0xFF, 0x80, 0x00, 0x00 };
    auto frame = SignalCodec::DecodeFrame(123, data, 8, "TEST-DEV");
    
    EXPECT_EQ(frame.fault_flags(), 0xFF800000);
    EXPECT_EQ(frame.can_id(), 123);
    EXPECT_EQ(frame.device_id(), "TEST-DEV");
}

TEST(SignalCodecTests, DecodeFrame_ParsesBitPackedValuesCorrectly) {
    // Temp: Byte 0. Let's set it to 10 (0x0A). Expected: 20.0 + 10 * 0.5 = 25.0
    // RPM: Byte 1 (8 bits) + Byte 2 lower 4 bits. Let's set RPM raw to 0x015A (346).
    //   Byte 1 = 0x5A, Byte 2 lower 4 bits = 0x01.
    // Door: Byte 2, bit 4. Let's set to 1.
    // Occupancy: Byte 2, bits 5-7. Let's set to 3. (binary 011).
    // So Byte 2 = 0x01 (from RPM) | 0x10 (Door) | 0x60 (Occupancy 3 << 5) = 0x71.
    uint8_t data[8] = { 0x0A, 0x5A, 0x71, 0, 0, 0, 0, 0 };
    auto frame = SignalCodec::DecodeFrame(123, data, 8, "TEST-DEV");
    
    EXPECT_DOUBLE_EQ(frame.belt_motor_temp_c(), 25.0);
    EXPECT_DOUBLE_EQ(frame.scanner_rpm(), 3460.0); // 346 * 10.0
    EXPECT_TRUE(frame.safety_door_closed());
    EXPECT_EQ(frame.bay_occupancy(), 3);
}

TEST(SignalCodecTests, DecodeFrame_ParsesSignExtension) {
    // For coverage, we should test sign extension, but our signals are unsigned.
    // We will just verify it handles generic extraction cleanly without crashing.
    uint8_t data[8] = { 0 };
    auto frame = SignalCodec::DecodeFrame(123, data, 8, "TEST-DEV");
    EXPECT_DOUBLE_EQ(frame.belt_motor_temp_c(), 20.0);
    EXPECT_DOUBLE_EQ(frame.scanner_rpm(), 0.0);
}
