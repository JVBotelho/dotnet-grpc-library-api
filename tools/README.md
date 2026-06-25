# Tools

This directory contains utility scripts for local development and testing.

## CAN Bus Generator

`can-generator.py` is a Python script that generates mock CAN-FD frames. It supports both native Linux virtual CAN (`vcan0`) and a cross-platform UDP Emulator. This allows testing the C++ Kiosk client locally or in Docker without physical CAN hardware.

> **Note — local UDP vs. real CAN in CI:** `vcan0` (SocketCAN) only exists on a Linux kernel with the `vcan` module loaded, which the default **WSL2 / Docker Desktop kernel does not include** — so `modprobe vcan` fails out-of-the-box on Windows/macOS. For local development use the **UDP Emulator (Setup A)**: it sends the *same* binary CAN-FD frames, just over UDP instead of the kernel bus, so the C++ decode path is identical. The genuine `vcan0` / `CAN_RAW_FD_FRAMES` path (Setup B) is verified end-to-end in CI, where the runner installs `linux-modules-extra-$(uname -r)` and brings up `vcan0` — see [`scripts/e2e-test.sh`](../scripts/e2e-test.sh).

### Setup A: Cross-Platform (Docker / Windows / Mac)

You can run the CAN generator inside an ephemeral Docker container to send data over UDP to the Kiosk container running on your local Docker network:

```bash
docker run --rm -v ${PWD}/tools:/tools python:3.10-slim sh -c "pip install cantools && python /tools/can-generator.py udp:host.docker.internal:5555"
```

### Setup B: Native Linux (`vcan0`)

1. Load the vcan module:
```bash
sudo modprobe vcan
```

2. Add a virtual CAN interface:
```bash
sudo ip link add dev vcan0 type vcan
sudo ip link set up vcan0
```

3. Run the generator natively:
```bash
python3 can-generator.py vcan0
```

## Running the C++ Kiosk Client

The C++ Kiosk code is located in `clients/cpp/`. The client is designed to run as a bare-metal daemon on physical IoT hardware.

**To run the kiosk in Docker (Recommended):**
We provide a `library-kiosk` profile in `compose.yaml` and `compose.prod.yaml`. It automatically provisions the mTLS certificates and handles the networking.
```bash
docker compose -f compose.prod.yaml --profile kiosk up -d
```

**To run the kiosk natively (Advanced):**
1. Build the C++ client natively using CMake and vcpkg (binary generated at `clients/cpp/build/kiosk`).
2. Generate a client certificate for your testing device ID using the `gen-dev-ca.sh` script.
3. Configure the environment variables (`KIOSK_SERVER_ENDPOINT`, `KIOSK_DEVICE_ID`, `KIOSK_USE_TLS=1`, `KIOSK_API_KEY`) to point to your `library-grpc` instance.
4. Execute `./clients/cpp/build/kiosk`.
