#include <gtest/gtest.h>
#include "CanReader.h"
#include "kiosk.pb.h"
#include <atomic>
#include <thread>

using namespace LibrarySystem::Contracts::Protos;

class CanReaderTests : public ::testing::Test {
protected:
    void SetUp() override {
    }

    void TearDown() override {
    }
};

TEST_F(CanReaderTests, ConstructorAndDestructor) {
    // Just testing that it doesn't crash on init/destroy
    CanReader reader("vcan0", "DEVICE-001");
}

TEST_F(CanReaderTests, StartWithInvalidInterface_FailsGracefully) {
    CanReader reader("invalid_vcan_999", "DEVICE-001");
    
    bool result = reader.Start([](const DeviceFrame&) {});
    
    // It should fail either because we are not root or the interface doesn't exist
    EXPECT_FALSE(result);
}

TEST_F(CanReaderTests, StopWithoutStart_Safe) {
    CanReader reader("vcan0", "DEVICE-001");
    reader.Stop(); // Should not crash
}
