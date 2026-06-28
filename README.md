# 📚 Library System - Enterprise .NET 10 Microservices

![.NET 10](https://img.shields.io/badge/.NET%2010-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?style=for-the-badge&logo=postgresql&logoColor=white)
![gRPC](https://img.shields.io/badge/gRPC-244c5a?style=for-the-badge&logo=grpc&logoColor=white)
![Coraza WAF](https://img.shields.io/badge/Coraza_WAF-Protected-green?style=for-the-badge&logo=shield&logoColor=white)
![Tests](https://img.shields.io/github/actions/workflow/status/JVBotelho/dotnet-grpc-library-api/main.yml?label=Tests&logo=github&style=for-the-badge)

A high-performance, distributed library management system designed to demonstrate **Modern Software Architecture** principles.

This project goes beyond simple CRUD, implementing **Domain-Driven Design (DDD)**, **CQRS**, and **Clean Architecture** to solve complex business rules (inventory management, lending logic, and historical analytics) within a distributed **gRPC** environment.

---

## 🏗️ Architecture & Design Patterns

The solution is split into distinct services communicating via high-performance RPC, protected by an edge security layer:

1.  **Library.Waf (Edge Security):** A **Web Application Firewall** using Caddy + Coraza. It acts as the entry point, providing DDoS protection, rate limiting, and deep packet inspection before traffic reaches the application.
2.  **Library.Api (Gateway):** A thin REST API acting as a Backend-for-Frontend (BFF). It handles HTTP requests, validation, and forwards commands to the core via gRPC.
3.  **Library.Grpc (Core):** The heart of the system. It encapsulates the Domain and Application layers, manages the Database, and executes business logic.

### Key Concepts Applied

* **Clean Architecture:** Strict dependency rule (Domain <- Application <- Infrastructure <- Presentation).
* **Domain-Driven Design (DDD):** Rich Domain Models (`Book`, `LendingActivity`) enforce invariants. No anemic models allowed.
* **CQRS (Command Query Responsibility Segregation):** Implemented using **MediatR**. Writes (Commands) and Reads (Queries) are handled separately for scalability.
* **Vertical Slices:** Features are organized by Use Cases (e.g., `BorrowBook`, `GetMostBorrowed`) rather than technical layers.
* **gRPC Code-First:** Strongly typed contracts defined in `.proto` files shared between services.
* **Shift-Left Quality:** Heavy emphasis on unit testing domain logic and integration testing API contracts.

---

## 🛡️ Security Architecture (WAF)

Security is a first-class citizen in this architecture. The system is protected by a next-generation WAF implementation tailored for .NET microservices.

* **Engine:** **Coraza** (Golang port of ModSecurity) running on top of **Caddy Reverse Proxy**.
* **Ruleset:** **OWASP Core Rule Set (CRS) v4.0.0 (Stable Release)**. We utilize the official v4 release ruleset to ensure protection against modern attack vectors, including specific defenses for Multipart Uploads (CVE-202X mitigation) and Generic RCEs.
* **Configuration:**
    * **Paranoia Level 2 (PL2):** Elevated security posture that inspects deeper into request payloads, blocking advanced SQL Injection and XSS patterns that standard WAFs miss.
    * **.NET Tuning:** Custom exclusion rules (`REQUEST-900`) implemented to handle ASP.NET Core specifics (Antiforgery Tokens, SignalR, Health Checks) preventing false positives without compromising security.
    * **DDoS Mitigation:** Layer 7 Rate Limiting configured per client IP.

---

## 🚀 Tech Stack

* **Runtime:** .NET 10 (Preview/LTS)
* **Edge/Security:** Caddy, Coraza WAF, OWASP CRS v4
* **Communication:** gRPC (HTTP/2) & REST (HTTP/1.1)
* **Data:** PostgreSQL 16
* **ORM:** Entity Framework Core (Code-First with Fluent API)
* **Mediation:** MediatR
* **Testing:** xUnit, FluentAssertions, Moq, AutoFixture
* **Containerization:** Docker & Docker Compose

---

## 🔍 Inspector (WPF Desktop Tool)

A standalone WPF desktop application for real-time topology exploration and WAF log monitoring. Designed as a portfolio showcase of WPF/MVVM architecture — documented in [ADR-002](docs/ADR-002-WPF-Client-Architecture.md).

### Features

- **Graph canvas** — live topology of books and authors fetched via gRPC, rendered as interactive draggable nodes with typed colour-coded rails (violet for books, amber for authors)
- **Inspector panel** — click any node to inspect and inline-edit book metadata; changes are persisted through gRPC → PostgreSQL round-trip
- **WAF console** — one-click streaming of WAF audit logs from the Coraza layer with severity dot indicators (info / warning / critical)
- **Toast notifications** — non-blocking feedback for every service call outcome

### Architecture (ADR-002)

| Layer | Details |
|---|---|
| Presentation | WPF + `CommunityToolkit.Mvvm` 8.4 (source-generated `[ObservableProperty]` / `[RelayCommand]`) |
| Services | `IGraphDataService` / `ILogTailerService` interfaces — decoupled from gRPC clients, fully mockable |
| Messaging | `WeakReferenceMessenger` for node-selection events (no MainWindow coupling) |
| Notifications | `INotificationService` singleton — auto-dismissing toast overlay, no `MessageBox.Show` in ViewModels |
| Theming | `InspectorTheme.xaml` ResourceDictionary — electric violet `#7C16FF` design tokens |
| Tests | `LibrarySystem.Tools.UnitTests` — xUnit + Moq, targeting `net10.0-windows` |

### Running the Inspector

Requires Docker running (the Inspector connects to the gRPC service on `localhost:5001`).

> [!NOTE]
> **Standalone Desktop App:** The WPF Inspector is fully compatible with both Development and Production Docker environments. However, since it is a native Windows Desktop application, it is **not** distributed alongside the backend Docker images in `ghcr.io`. To use it, you must open the solution in Visual Studio / Rider on a Windows machine and compile it from source.

```bash
docker-compose up -d
# Then launch LibrarySystem.Tools from Visual Studio / Rider
```

---

## 🛠️ Getting Started

### Prerequisites

* Docker & Docker Compose

### Running the Application

You don't need .NET installed to run the system. Docker handles everything.

1.  **Start the System:**
    To build the images locally from source (Development mode):
    ```bash
    docker-compose up --build
    ```

    **Or**, to use the **pre-built production images** downloaded directly from GitHub Container Registry (Recruiter / Production mode):
    ```bash
    docker-compose -f compose.prod.yaml up
    ```

2.  **Access the System:**
    * **Public API (WAF Protected):** `http://localhost:80`
    * **Swagger UI:** `http://localhost:80/swagger`
    * **WAF Logs:** `./logs/waf/audit.json`
    * **gRPC Service:** `http://localhost:5001` (Internal Docker Network)

    *Note: The system automatically seeds the database with sample books and historical lending data on startup.*

3.  **Simulating Edge Devices (C++ Kiosk):**
    The system includes a C++ edge device emulator that generates real-time telemetry via CAN bus and forwards it to the gRPC server using mTLS and API keys. 
    By default, it is **isolated** to prevent spamming your database during normal testing. 
    To start the infrastructure *along* with the kiosk using pre-built images:
    ```bash
    docker-compose -f compose.prod.yaml --profile kiosk up
    ```
    Once the Kiosk is running and listening on UDP port 5555, you can generate telemetry packets (simulating hardware CAN frames). You can run this directly via Docker without needing Python installed locally:
    ```bash
    docker run --rm -v ${PWD}/tools:/tools python:3.10-slim sh -c "pip install cantools && python /tools/can-generator.py udp:host.docker.internal:5555"
    ```
    You will see the events flowing through the gRPC server, processed by MediatR, and stored in PostgreSQL!

    > **Why UDP locally, but real SocketCAN in CI?**
    > `SocketCAN`/`vcan0` is a Linux-kernel facility and is **not** compiled into the default WSL2 / Docker Desktop kernel on Windows or macOS. To keep the "runs in 30 seconds on any machine" property, the local demo feeds the kiosk over a **UDP transport shim** that carries **byte-identical CAN-FD frames** (DBC-encoded via `cantools`, packed to the 72-byte `canfd_frame` layout). The C++ decode path (`SignalCodec`, bit-level Intel/Motorola extraction) is the same in both modes — UDP is a transport, not a simulation.
    > The genuine `vcan0` / `CAN_RAW_FD_FRAMES` path is exercised **end-to-end in CI**: GitHub Actions installs `linux-modules-extra`, brings up `vcan0` at MTU 72, and asserts the full `vcan0 → C++ → mTLS gRPC → C# → PostgreSQL → STOP_MOTOR` round-trip. See [`scripts/e2e-test.sh`](scripts/e2e-test.sh) and [ADR-003](Docs/adr/003-cpp-edge-device-can-telemetry.md).

---

## 🧪 Running Tests

We prioritize **Developer Experience**. You can run the entire test suite (Unit + Integration) inside a container without setting up a local environment.

### Test Strategy

* **Domain Tests:** Verify complex business rules (e.g., "Cannot borrow if copies < 1") in isolation.
* **Application Tests:** Verify the orchestration of Use Cases and Repository calls using Mocks.
* **Integration Tests:** Verify the API Gateway correctly maps HTTP requests to gRPC calls using `WebApplicationFactory` and gRPC Mocks.

### Command to Run Tests

```bash
# Windows / Linux / Mac
docker-compose -f docker-compose.tests.yml up --build --abort-on-container-exit