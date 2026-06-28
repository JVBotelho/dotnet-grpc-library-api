#pragma once
#include <string>

class ImageFeatureExtractor {
public:
    static std::string ComputePHash(const std::string& image_data);
};
