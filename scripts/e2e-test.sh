#!/usr/bin/env bash
set -e

echo "=== End-to-End Integration Test ==="

CAN_INTERFACE="vcan0"

# 1. Setup Virtual CAN Interface
# NOTE: stderr is intentionally NOT suppressed — if vcan setup fails we want the
# real reason (module missing, mtu rejected, etc.) visible in the CI log instead
# of a silent fallback.
echo "Setting up vcan0..."
if sudo ./clients/cpp/scripts/setup-vcan.sh; then
    echo "✅ vcan0 setup successful — running E2E over real SocketCAN/CAN-FD."
else
    echo "⚠️ vcan0 setup failed (see output above for the reason). Falling back to UDP Emulator on port 5555."
    CAN_INTERFACE="udp:5555"
fi

# 2. Install Python dependencies
echo "Installing Python dependencies..."
python3 -m pip install cantools --break-system-packages || python3 -m pip install cantools

# 3. Generate Dev Certificates (if needed by compose)
echo "Running dev certificate generation..."
chmod +x scripts/gen-dev-ca.sh || true
./scripts/gen-dev-ca.sh || true

# 4. Start backend services
export PFX_PASSWORD=password
echo "Injecting CI Dockerfile for cpp-compute..."
cp services/cpp-compute/Dockerfile.ci services/cpp-compute/Dockerfile
echo "Starting Docker Compose services..."
docker compose up -d db library-grpc library-api library-compute waf

# 5. Wait for services
echo "Waiting for database and API to be fully ready (20s)..."
sleep 20

echo "--- RUNNING FULL E2E TEST (CAN/UDP -> C++ -> gRPC -> C# -> DB) ---"

INITIAL_COUNT=$(docker exec library-db psql -U user -d librarydb -t -c "SELECT COUNT(*) FROM \"ProcessedEvents\"" | tr -d ' ' || echo "0")
echo "Initial event count in database: $INITIAL_COUNT"

export KIOSK_SERVER_ENDPOINT="127.0.0.1:8443"
export KIOSK_DEVICE_ID="KIOSK-001"
export KIOSK_USE_TLS="1"
export KIOSK_ROOT_CERTS_PATH="certs/ca.crt"
export KIOSK_CERT_CHAIN_PATH="certs/client.crt"
export KIOSK_PRIVATE_KEY_PATH="certs/client.key"
export KIOSK_API_KEY="secret-kiosk-key"

export GRPC_VERBOSITY=DEBUG
export GRPC_TRACE=connectivity_state,secure_endpoint,transport_security

echo "Connecting to Kiosk Server at $KIOSK_SERVER_ENDPOINT as $KIOSK_DEVICE_ID"
./clients/cpp/build/kiosk --can-link $CAN_INTERFACE > kiosk.log 2>&1 &
KIOSK_PID=$!
sleep 3

echo "Starting CAN Generator on $CAN_INTERFACE to send frames..."
python3 tools/can-generator.py $CAN_INTERFACE &
CAN_PID=$!

sleep 10

echo "=== WAF LOGS ==="
docker compose logs waf
echo "=== GRPC LOGS ==="
docker compose logs library-grpc
echo "=== KIOSK LOGS ==="
cat kiosk.log

echo "Stopping clients..."
kill $CAN_PID || true
kill $KIOSK_PID || true
wait $KIOSK_PID 2>/dev/null || true

sleep 5

FINAL_COUNT=$(docker exec library-db psql -U user -d librarydb -t -c "SELECT COUNT(*) FROM \"ProcessedEvents\"" | tr -d ' ' || echo "0")
echo "Final event count in database: $FINAL_COUNT"

docker compose down

if [ "$FINAL_COUNT" -gt "$INITIAL_COUNT" ]; then
    echo "✅ Telemetry was processed successfully ($INITIAL_COUNT -> $FINAL_COUNT)."
    
    if grep -q "Kind=1" kiosk.log; then
        echo "✅ E2E TEST PASSED: STOP_MOTOR (Kind=1) command was successfully received back from the server!"
        exit 0
    else
        echo "❌ E2E TEST FAILED: STOP_MOTOR command was not found in Kiosk logs."
        exit 1
    fi
else
    echo "❌ E2E TEST FAILED: No telemetry events were processed."
    exit 1
fi
