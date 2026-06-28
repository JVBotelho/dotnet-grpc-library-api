#include <iostream>
#include <memory>
#include <string>
#include <iomanip>
#include <sstream>

#include <grpcpp/grpcpp.h>
#include "compute.grpc.pb.h"
#include "ImageFeatureExtractor.h"
#include <regex>

using grpc::Server;
using grpc::ServerBuilder;
using grpc::ServerContext;
using grpc::Status;
using grpc::StatusCode;
using LibrarySystem::Contracts::Protos::Compute;
using LibrarySystem::Contracts::Protos::ComputeNodeImageHashRequest;
using LibrarySystem::Contracts::Protos::ComputeNodeImageHashResponse;

class ComputeServiceImpl final : public Compute::Service {
    Status ComputeImageHash(ServerContext* context, const ComputeNodeImageHashRequest* request, ComputeNodeImageHashResponse* reply) override {
        // [REVIEW-H-5] Validate image_data size before allocation
        if (request->image_data().size() > 5 * 1024 * 1024) { // 5MB max payload
            return Status(StatusCode::INVALID_ARGUMENT, "Image data exceeds maximum allowed size (5MB)");
        }

        std::string hash = ImageFeatureExtractor::ComputePHash(request->image_data());
        reply->set_p_hash(hash);

        // [APPSEC-H-4] Sanitize image_id logging to prevent CRLF injection
        std::string safe_id = std::regex_replace(request->image_id(), std::regex("[\\r\\n]"), "_");
        std::cout << "Computed hash for image " << safe_id << ": " << hash << std::endl;
        
        return Status::OK;
    }
};

void RunServer() {
    std::string server_address("0.0.0.0:50052");
    ComputeServiceImpl service;

    ServerBuilder builder;
    // [APPSEC-L-5] Set MaxReceiveMessageSize to 10MB
    builder.SetMaxReceiveMessageSize(10 * 1024 * 1024);
    
    // Insecure for internal cluster traffic
    builder.AddListeningPort(server_address, grpc::InsecureServerCredentials());
    builder.RegisterService(&service);

    std::unique_ptr<Server> server(builder.BuildAndStart());
    std::cout << "Compute Server listening on " << server_address << std::endl;
    server->Wait();
}

int main(int argc, char** argv) {
    RunServer();
    return 0;
}
