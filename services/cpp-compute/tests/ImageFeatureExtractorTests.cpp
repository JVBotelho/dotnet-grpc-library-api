#include <gtest/gtest.h>
#include "../src/ImageFeatureExtractor.h"

TEST(ImageFeatureExtractorTests, ComputePHash_ComputesCorrectHash) {
    std::string data = "test";
    std::string hash = ImageFeatureExtractor::ComputePHash(data);
    EXPECT_EQ(hash, "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08");
}
