#include "ImageFeatureExtractor.h"
#include <iomanip>
#include <sstream>
#include <openssl/sha.h>

std::string ImageFeatureExtractor::ComputePHash(const std::string& image_data) {
    unsigned char hash[SHA256_DIGEST_LENGTH];
    SHA256(reinterpret_cast<const unsigned char*>(image_data.c_str()), image_data.size(), hash);

    std::stringstream ss;
    for (int i = 0; i < SHA256_DIGEST_LENGTH; ++i) {
        ss << std::hex << std::setfill('0') << std::setw(2) << static_cast<int>(hash[i]);
    }
    return ss.str();
}
