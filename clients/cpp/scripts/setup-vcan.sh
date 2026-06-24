#!/usr/bin/env bash
set -e

echo "Setting up vcan0 with CAN-FD support..."
sudo modprobe vcan
sudo ip link add dev vcan0 type vcan || echo "vcan0 already exists"
# CRITICAL: mtu 72 is required for CAN_RAW_FD_FRAMES to work
sudo ip link set vcan0 mtu 72
sudo ip link set up vcan0
echo "vcan0 is ready."
