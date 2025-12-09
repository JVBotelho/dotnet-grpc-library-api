# ADR 001: Edge Security Strategy with Caddy and Coraza WAF

## Status
Accepted

## Context
The Library System exposes public REST and gRPC APIs that require robust protection against Layer 7 attacks (SQL Injection, XSS, RCE) and Application Layer DDoS.

Standard .NET middleware provides some protection, but a dedicated Web Application Firewall (WAF) is required at the edge to inspect traffic before it reaches the application resources. The solution must support the OWASP Core Rule Set (CRS) and integrate seamlessly into a containerized, cloud-native architecture.

## Decision
We will utilize **Caddy** as the reverse proxy and edge gateway, integrating the **Coraza WAF** plugin (a native Go implementation of ModSecurity).

This replaces the traditional Nginx + ModSecurity v3 stack.

## Detailed Justification

1.  **Memory Safety & Modern Architecture:**
    Unlike Nginx/ModSecurity (written in C/C++), Caddy and Coraza are written in **Go**. This provides inherent memory safety, eliminating entire classes of vulnerabilities (such as buffer overflows) within the security layer itself.

2.  **Long-term Viability (ModSecurity EOL):**
    Trustwave announced the **End of Life (EOL)** for commercial support of ModSecurity effective July 1, 2024. As the legacy project enters maintenance mode with uncertain community velocity, adopting Coraza ensures we are building on an actively maintained, modern engine designed for the cloud-native era.

3.  **Performance & Integration:**
    Coraza runs in-process within Caddy as a native Go module. This eliminates the latency overhead associated with bridging C++ libraries or using sidecar proxies, resulting in higher throughput for high-performance .NET microservices.

4.  **Rule Set Compatibility:**
    Coraza fully supports the **OWASP Core Rule Set (CRS) v4**, allowing us to leverage bleeding-edge protection rules (e.g., mitigation for CVE-202X series and Multipart Upload attacks) that are challenging to configure in legacy environments.

5.  **Developer Experience (DX):**
    Caddy's configuration (`Caddyfile`) is significantly more concise and readable than `nginx.conf`. It also supports automatic HTTPS and native rate-limiting without requiring complex third-party modules.

## Consequences

### Positive
* **Future-Proofing:** mitigates the risk of running unmaintained security software (ModSecurity EOL).
* **Enhanced Security:** Access to CRS v4 "dev" branch rules ensures protection against modern attack vectors.
* **Simplified Ops:** A single container binary handles Proxy, WAF, and Rate Limiting.
* **Observability:** Native structured JSON logging makes audit trails easier to parse.

### Negative / Risks
* **Build Complexity:** Requires a custom Docker build using `xcaddy` to compile the WAF plugin into the server, rather than simply pulling a standard image.
* **Tuning Requirements:** Strict WAF rules (Paranoia Level 2) require careful tuning (Exclusion Rules 900) to prevent false positives with ASP.NET Core features like SignalR and Antiforgery Tokens.