cmake_minimum_required(VERSION 3.22)
project(kiosk-device VERSION 0.1.0 LANGUAGES CXX)

# Force C++23 as per implementation plan
set(CMAKE_CXX_STANDARD 23)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

# Find packages provided by vcpkg
find_package(gRPC CONFIG REQUIRED)
find_package(Protobuf CONFIG REQUIRED)

# Include custom protobuf generation module
include(cmake/Protobuf.cmake)

# Generate grpc and protobuf sources from our shared proto
# The path is relative to the cpp client root
set(PROTO_SRC_DIR "${CMAKE_CURRENT_SOURCE_DIR}/../../LibrarySystem.Contracts/Protos")
set(PROTO_FILE "${PROTO_SRC_DIR}/kiosk.proto")

# Call custom function to generate cpp and grpc.cpp files
generate_protobuf_and_grpc(
    "${PROTO_FILE}"
    "${PROTO_SRC_DIR}"
    PROTO_SRCS
    PROTO_HDRS
    GRPC_SRCS
    GRPC_HDRS
)

add_executable(kiosk
    src/main.cpp
    src/config.cpp
    src/KioskClient.cpp
    src/CanReader.cpp
    src/OfflineStore.cpp
    src/DeviceLink.cpp
    src/SignalCodec.cpp
    ${PROTO_SRCS}
    ${PROTO_HDRS}
    ${GRPC_SRCS}
    ${GRPC_HDRS}
)

target_include_directories(kiosk PRIVATE 
    ${CMAKE_CURRENT_BINARY_DIR} # for generated headers
    src
)

target_link_libraries(kiosk PRIVATE
    gRPC::grpc++
    protobuf::libprotobuf
    sqlite3
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

add_executable(kiosk_tests
    tests/OfflineStoreTests.cpp
    tests/SignalCodecTests.cpp
    tests/CanReaderTests.cpp
    tests/OfflineIntegrationTests.cpp
    src/CanReader.cpp
    src/OfflineStore.cpp
    src/SignalCodec.cpp
    ${PROTO_SRCS}
    ${PROTO_HDRS}
)

target_include_directories(kiosk_tests PRIVATE 
    ${CMAKE_CURRENT_BINARY_DIR}
    src
)

target_link_libraries(kiosk_tests PRIVATE
    GTest::gtest_main
    protobuf::libprotobuf
    sqlite3
)

add_test(NAME KioskTests COMMAND kiosk_tests)

# Benchmarks
find_package(benchmark CONFIG REQUIRED)

add_executable(kiosk_benchmarks
    bench/OfflineStoreBench.cpp
    src/OfflineStore.cpp
    ${PROTO_SRCS}
    ${PROTO_HDRS}
)

target_include_directories(kiosk_benchmarks PRIVATE 
    ${CMAKE_CURRENT_BINARY_DIR}
    src
)

target_link_libraries(kiosk_benchmarks PRIVATE
    benchmark::benchmark_main
    protobuf::libprotobuf
    sqlite3
)
