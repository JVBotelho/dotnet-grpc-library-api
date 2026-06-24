# cpp-compute Service

This is the C++ Compute Node for the Library System, responsible for heavy computational tasks such as image hashing, perceptual hash extraction, and anomaly detection in returned items.

## Architecture & Dependencies

- **Framework:** gRPC (C++)
- **Concurrency:** Thread-per-request (or async depending on gRPC setup)
- **Hardware Dependency:** This node heavily relies on GPU acceleration for tensor operations. It requires **NVIDIA CUDA** architecture (Compute Capability 7.0 or higher recommended). Running this service on a CPU-only host will result in severe performance degradation and potential timeouts during image processing.

## Future Plans (Phase 7)

In Phase 7, this service will be integrated directly into the deployment pipeline with automated test coverage enforcement. 
Code coverage metrics (C++) will be collected alongside C# metrics to ensure the minimum 60% coverage threshold is met across the entire codebase.

### Security Notes
- Inputs over 5MB are rejected to prevent resource exhaustion.
- Image IDs are sanitized before logging to mitigate CRLF log injection vulnerabilities.
