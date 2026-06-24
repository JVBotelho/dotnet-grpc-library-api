cmake_minimum_required(VERSION 3.22)
project(cpp-compute VERSION 0.1.0 LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 23)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

find_package(gRPC CONFIG REQUIRED)
find_package(Protobuf CONFIG REQUIRED)
find_package(OpenSSL REQUIRED)

include(cmake/Protobuf.cmake)

set(PROTO_SRC_DIR "${CMAKE_CURRENT_SOURCE_DIR}/../../LibrarySystem.Contracts/Protos")
set(PROTO_FILE "${PROTO_SRC_DIR}/compute.proto")

generate_protobuf_and_grpc(
    "${PROTO_FILE}"
    "${PROTO_SRC_DIR}"
    PROTO_SRCS
    PROTO_HDRS
    GRPC_SRCS
    GRPC_HDRS
)

add_executable(cpp_compute
    src/main.cpp
    src/ImageFeatureExtractor.cpp
    ${PROTO_SRCS}
    ${PROTO_HDRS}
    ${GRPC_SRCS}
    ${GRPC_HDRS}
)

target_include_directories(cpp_compute PRIVATE 
    ${CMAKE_CURRENT_BINARY_DIR}
    src
)

target_link_libraries(cpp_compute PRIVATE
    gRPC::grpc++
    protobuf::libprotobuf
    OpenSSL::Crypto
)

# Testing
enable_testing()
find_package(GTest CONFIG REQUIRED)

# Add code coverage compiler flags if requested
option(ENABLE_COVERAGE "Enable code coverage reporting" OFF)
if(ENABLE_COVERAGE AND CMAKE_CXX_COMPILER_ID MATCHES "GNU|Clang")
    add_compile_options(--coverage -O0 -g -fsanitize=address,undefined)
    add_link_options(--coverage -fsanitize=address,undefined)
endif()

add_executable(cpp_compute_tests
    tests/ImageFeatureExtractorTests.cpp
    src/ImageFeatureExtractor.cpp
)

target_include_directories(cpp_compute_tests PRIVATE 
    ${CMAKE_CURRENT_BINARY_DIR}
    src
)

target_link_libraries(cpp_compute_tests PRIVATE
    GTest::gtest_main
    OpenSSL::Crypto
)

add_test(NAME CppComputeTests COMMAND cpp_compute_tests)
