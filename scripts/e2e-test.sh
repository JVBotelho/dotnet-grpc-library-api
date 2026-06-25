#!/usr/bin/env bash
set -e

echo "=== End-to-End Integration Test ==="

# 1. Setup Virtual CAN Interface
echo "Setting up vcan0..."
sudo ./clients/cpp/scripts/setup-vcan.sh

# 2. Install Python dependencies
echo "Installing Python dependencies..."
python3 -m pip install cantools --break-system-packages || python3 -m pip install cantools

# 3. Generate Dev Certificates (if needed by compose)
echo "Running dev certificate generation..."
chmod +x scripts/gen-dev-ca.sh || true
./scripts/gen-dev-ca.sh || true

# 4. Start backend services
echo "Starting Docker Compose services..."
docker compose up -d db library-grpc library-api library-compute waf

# 5. Wait for services
echo "Waiting for database and API to be fully ready (20s)..."
sleep 20

# 6. Check initial DB state
INITIAL_COUNT=$(docker exec library-db psql -U user -d librarydb -t -c "SELECT COUNT(*) FROM \"ProcessedEvents\"" | tr -d ' ' || echo "0")
echo "Initial event count in database: $INITIAL_COUNT"

# 7. Start Kiosk Client
echo "Starting C++ Kiosk Client..."
export KIOSK_SERVER_ENDPOINT="localhost:5001"
export KIOSK_USE_TLS=0
export KIOSK_DEVICE_ID="E2E-TEST-KIOSK"

# Run in background
./clients/cpp/build/kiosk --can-link vcan0 &
KIOSK_PID=$!

sleep 3

# 8. Start CAN Generator
echo "Starting CAN Generator to send frames..."
python3 tools/can-generator.py vcan0 &
CAN_PID=$!

# Run for 10 seconds to generate a few frames
sleep 10

# 9. Stop Clients
echo "Stopping clients..."
kill $CAN_PID || true
kill $KIOSK_PID || true
wait $KIOSK_PID 2>/dev/null || true

# Give backend a moment to process the queue
sleep 5

# 10. Check final DB state
FINAL_COUNT=$(docker exec library-db psql -U user -d librarydb -t -c "SELECT COUNT(*) FROM \"ProcessedEvents\"" | tr -d ' ' || echo "0")
echo "Final event count in database: $FINAL_COUNT"

# 11. Cleanup
echo "Cleaning up environment..."
docker compose down

# 12. Assert success
if [ "$FINAL_COUNT" -gt "$INITIAL_COUNT" ]; then
    echo "✅ E2E TEST PASSED: Processed events increased from $INITIAL_COUNT to $FINAL_COUNT."
    exit 0
else
    echo "❌ E2E TEST FAILED: No telemetry events were processed."
    exit 1
fi
