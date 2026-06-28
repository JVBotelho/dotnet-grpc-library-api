FROM mcr.microsoft.com/devcontainers/cpp:1-ubuntu-22.04 AS builder

# Install build dependencies
RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
    && apt-get -y install --no-install-recommends cmake build-essential ninja-build pkg-config \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# Install vcpkg
ENV VCPKG_ROOT=/opt/vcpkg
RUN git clone https://github.com/microsoft/vcpkg.git $VCPKG_ROOT \
    && cd $VCPKG_ROOT \
    && ./bootstrap-vcpkg.sh

# Copy proto definitions
WORKDIR /app/LibrarySystem.Contracts
COPY LibrarySystem.Contracts/Protos/*.proto ./Protos/

# Build kiosk client
WORKDIR /app/clients/cpp
COPY clients/cpp .

# Configure and build
RUN cmake -B build -S . -DCMAKE_TOOLCHAIN_FILE=$VCPKG_ROOT/scripts/buildsystems/vcpkg.cmake
RUN cmake --build build --config Release

FROM ubuntu:22.04
WORKDIR /app
COPY --from=builder /app/clients/cpp/build/kiosk .

ENTRYPOINT ["./kiosk", "--can-link", "udp:5555"]
