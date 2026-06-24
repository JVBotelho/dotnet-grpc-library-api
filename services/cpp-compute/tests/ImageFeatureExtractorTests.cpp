#include <gtest/gtest.h>
#include "../src/ImageFeatureExtractor.h"

TEST(ImageFeatureExtractorTests, ComputePHash_ComputesCorrectHash) {
    std::string data = "test";
    // t=116, e=101, s=115, t=116. Sum = 448 = 0x1C0
    std::string hash = ImageFeatureExtractor::ComputePHash(data);
    EXPECT_EQ(hash, "00000000000001c0");
}
