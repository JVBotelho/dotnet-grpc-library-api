# Tools

This directory contains utility scripts for local development and testing.

## CAN Bus Generator

`can-generator.py` is a Python script that generates mock CAN-FD frames over a virtual CAN interface (`vcan0`) on Linux. This allows testing the C++ Kiosk client locally without physical CAN hardware.

### Setup (Linux only)

1. Load the vcan module:
```bash
sudo modprobe vcan
```

2. Add a virtual CAN interface:
```bash
sudo ip link add dev vcan0 type vcan
sudo ip link set up vcan0
```

3. Run the generator:
```bash
python3 can-generator.py vcan0
```

## Running the C++ Kiosk Client

The Kiosk client is designed to run as a bare-metal daemon on physical IoT hardware. We removed the Docker `library-kiosk` profile from `compose.yaml` to enforce TLS (`KIOSK_USE_TLS=1`) natively with a properly provisioned device certificate.

**To run the kiosk locally for testing:**
1. Build the C++ client natively using CMake and vcpkg.
2. Generate a client certificate for your testing device ID (e.g., `KIOSK-DEV-001`) using the `gen-dev-ca.sh` script.
3. Configure the environment variables (`KIOSK_SERVER_ENDPOINT`, `KIOSK_DEVICE_ID`, `KIOSK_USE_TLS=1`, etc.) to point to your `library-grpc` instance.
4. Execute `./library_kiosk_client`.
