#!/usr/bin/env bash
set -euo pipefail

echo "Setting up vcan0 with CAN-FD support..."
echo "Kernel: $(uname -r)"

# The vcan module is not built into the default kernel on GitHub-hosted runners
# (linux-azure); it ships in linux-modules-extra. Install it on demand so the
# real SocketCAN path can run in CI without a custom kernel.
if ! modinfo vcan >/dev/null 2>&1; then
    echo "vcan module not present; installing linux-modules-extra-$(uname -r)..."
    sudo apt-get update
    sudo apt-get install -y "linux-modules-extra-$(uname -r)"
fi

sudo modprobe vcan
sudo ip link add dev vcan0 type vcan || echo "vcan0 already exists"
# CRITICAL: mtu 72 is required for CAN_RAW_FD_FRAMES to work
sudo ip link set vcan0 mtu 72
sudo ip link set up vcan0
echo "vcan0 is ready."
