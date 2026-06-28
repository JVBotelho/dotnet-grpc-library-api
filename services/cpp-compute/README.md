# cpp-compute Service

This is the C++ Compute Node for the Library System, responsible for heavy computational tasks such as image hashing, perceptual hash extraction, and anomaly detection in returned items.

## Architecture & Dependencies

- **Framework:** gRPC (C++)
- **Concurrency:** Thread-per-request (or async depending on gRPC setup)
- **Hardware Dependency (Current):** None. The current implementation uses CPU-bound cryptographic hashing (SHA-256 via OpenSSL) as a placeholder for the perceptual hash baseline.
- **Hardware Dependency (Target Phase 7):** Once anomaly detection and true perceptual tensor hashing are introduced, this node will require GPU acceleration (**NVIDIA CUDA**, Compute Capability 7.0+). Running the future version on a CPU-only host will cause performance degradation.

## Future Plans (Phase 7)

In Phase 7, this service will be integrated directly into the deployment pipeline with automated test coverage enforcement. Code coverage metrics (C++) will be collected alongside C# metrics.
Furthermore, the hashing logic will be replaced with a real ML tensor model for anomaly detection on book images, at which point the CUDA hardware requirement will take effect.

### Security Notes
- Inputs over 5MB are rejected to prevent resource exhaustion.
- Image IDs are sanitized before logging to mitigate CRLF log injection vulnerabilities.
