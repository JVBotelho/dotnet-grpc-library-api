FROM mcr.microsoft.com/devcontainers/cpp:1-ubuntu-22.04 AS builder

# Install build dependencies
RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
    && apt-get -y install --no-install-recommends cmake build-essential \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# Install vcpkg
ENV VCPKG_ROOT=/opt/vcpkg
RUN git clone https://github.com/microsoft/vcpkg.git $VCPKG_ROOT \
    && cd $VCPKG_ROOT \
    && git checkout 2024.02.14 \
    && ./bootstrap-vcpkg.sh

# Copy proto definitions
WORKDIR /app/LibrarySystem.Contracts
COPY LibrarySystem.Contracts/Protos/compute.proto ./Protos/

# Build compute server
WORKDIR /app/services/cpp-compute
COPY services/cpp-compute .

# Configure and build
RUN cmake -B build -S . -DCMAKE_TOOLCHAIN_FILE=$VCPKG_ROOT/scripts/buildsystems/vcpkg.cmake
RUN cmake --build build --config Release

FROM ubuntu:22.04
WORKDIR /app
COPY --from=builder /app/services/cpp-compute/build/cpp_compute .

EXPOSE 50052
ENTRYPOINT ["./cpp_compute"]
