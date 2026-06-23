# ADR 003: Heterogeneous C++ Edge Device (Self-Service Kiosk) with CAN-FD Telemetry over gRPC

## Status
Accepted

**Date:** 2026-06-23
**Deciders:** Solution owner (João Botelho)
**Related:** [ADR-001 — Edge Security Strategy](./001-waf-strategy.md) · [ADR-002 — WPF Desktop Client Architecture](./002-wpf-mvvm-client.md)

## Context

The Library System is, today, a single-language solution: every component (`LibrarySystem.Grpc` core, `LibrarySystem.Api` REST gateway, `LibrarySystem.Tools` WPF Inspector) is C#/.NET 10, talking to a Caddy + Coraza WAF (ADR-001) and PostgreSQL. The contract surface lives in one IDL file, `LibrarySystem.Contracts/Protos/library.proto`, exposing `service Library` (13 unary RPCs) and `service Security` (one server-streaming RPC, `WatchWafLogs`).

This ADR exists because the project is being positioned for a **C++/C# Application Developer (eBike)** role whose hard and "significant-plus" requirements are not yet demonstrated anywhere in the portfolio:

- **C++** professional development (the project is 100% C#).
- **Cross-platform client and server bindings** generated from a shared IDL — currently only one language consumes the `.proto`.
- **All RPC communication patterns** — unary, client-side, server-side, **and bidirectional** streaming. Today only unary + one server-stream exist.
- **CAN / CAN-FD** hands-on experience (a "significant plus" for the eBike/automotive domain).
- **Embedded-systems** mindset.
- **High-performance, low-latency** distributed systems, with the ability to identify and correct bottlenecks.

The decision is *how* to add a credible C++/embedded/CAN-FD story to this codebase without turning it into two disjoint projects, and without scope-creeping past "finishable to a senior bar."

### Forces at play

- **One coherent story beats a feature pile.** Reviewers reward a single, runnable narrative over four half-finished demos. The C++ work must attach to the *existing* backend, not stand beside it.
- **Credibility over mimicry.** A thread spitting fake hex strings reads as "I read about CAN." Real `SocketCAN` (virtual `vcan0`) with CAN-FD 64-byte frames and bit-level signal coding reads as "I have touched CAN." The gap between those two is the whole point of the exercise.
- **Single source of truth for contracts.** The strongest demonstration of "cross-platform bindings" is one `.proto` generating C# *and* C++ stubs — the IDL is the contract, the language is an implementation detail.
- **Minimum-viable-but-credible (carried from ADR-002).** Depth on the differentiators (CAN-FD, bidi, resilience) is worth more than breadth across every possible feature.
- **Reuse existing muscle.** The owner already has static `musl` cross-compilation (x86_64/aarch64) and raw wire-protocol experience from the `skewrun` Rust project; the embedded angle should lean on that rather than invent new tooling.

## Decision

We will introduce a **heterogeneous C++ edge device** — a simulated self-service library **kiosk / return station** — that acts as a native gRPC **client** of the existing C# core, plus a small set of new RPCs to serve it. The device is written in **modern C++ (C++20)**, generates its bindings from the **same shared `.proto`** as the C# server, reads a **real virtual CAN-FD bus (`SocketCAN` `vcan0`)**, and exercises **all four gRPC streaming patterns** in one coherent device lifecycle.

Concretely, the architecture is fixed as:

1. **Identity & scope.** A physical-kiosk simulator (`clients/cpp/`, an out-of-solution CMake project) — not a second backend. It is the *device* in a device↔cloud topology: the C# core remains the system of record; the kiosk is an edge node that authenticates users, ingests returns, and streams hardware telemetry.

2. **Shared IDL, multi-language bindings.** A new `kiosk.proto` (and `telemetry` messages) is added to `LibrarySystem.Contracts/Protos/`. The C# side generates via `Grpc.Tools`; the C++ side generates via `protoc` + the gRPC C++ plugin invoked from CMake. **No hand-written contracts** — both languages compile from byte-identical IDL. This is the literal demonstration of "define service interfaces using IDL like Protobuf" and "cross-platform client and server bindings."

3. **All four RPC patterns, mapped to real device actions:**
   - **Unary** — `ValidateMember` (card tap → membership check) before any session.
   - **Server-streaming** — already present (`Security.WatchWafLogs`); reused, not rebuilt.
   - **Client-streaming** — `BulkReturn`: a stack of books dropped on the return belt is streamed as scans, the server replies once with a reconciled summary.
   - **Bidirectional-streaming** — `DeviceLink`: the kiosk continuously streams decoded CAN-FD telemetry frames (belt-motor temperature, optical-scanner RPM, safety-door state, bay occupancy) while the server streams **control commands** back on the same channel (e.g. `STOP_MOTOR`, `RAISE_ALARM`, `THROTTLE_INTAKE`) — a closed-loop control demonstration.

4. **CAN-FD as a first-class concern (the differentiator).** The kiosk owns a `CanReader` that binds to a **`SocketCAN` `vcan0`** interface using the CAN-FD socket option (`CAN_RAW_FD_FRAMES`, 64-byte payloads). Signals are encoded/decoded at the **bit level in a DBC-style codec** (start bit, length, scale, offset, endianness), not parsed as text. A companion generator process injects frames via `cansend`/libsocketcan so the demo runs on any Linux host or in CI with no physical hardware. The decoded signals are mapped into Protobuf telemetry messages and pushed onto the `DeviceLink` stream.

5. **Resilience / store-and-forward.** The kiosk treats the network as unreliable (an eBike-grade assumption). When the gRPC channel is down, member-return and telemetry events are persisted to a local **SQLite** buffer (WAL mode). On reconnect, the buffer is drained through the **`SyncOfflineQueue` client-streaming RPC** with at-least-once delivery and idempotency keys, so the server can dedupe. This turns "robust and distributed applications / ensure reliability" from a claim into a mechanism.

6. **Secure device identity.** The device↔cloud link uses **mTLS**: the kiosk presents a client certificate; the server validates it and binds the session to a device ID. This reflects real IoT/automotive provisioning and is a stronger security signal than the transport defaults.

7. **Performance, measured.** Frame encode/decode and serialization hot paths are benchmarked with **Google Benchmark** (C++) and mirrored with **BenchmarkDotNet** (C#), reporting throughput and p99 latency. This is the evidence behind "high-performance, low-latency" and "identify and correct bottlenecks," and it rhymes with the existing `RASP.Net` benchmarking discipline.

8. **Visible payoff via the Inspector.** The WPF Inspector (ADR-002) gains a live **device telemetry view** fed by a server-streaming projection of `DeviceLink` data, so a reviewer *sees* battery/motor-style gauges move in real time rather than reading about them.

9. **Embedded delivery.** The C++ kiosk is **statically cross-compiled to `aarch64` (musl)** so it runs on a Raspberry-Pi-class target, reusing the toolchain approach proven in `skewrun`. A `docker compose` profile brings up the whole heterogeneous fleet (C# core + WAF + Postgres + `vcan0` + C++ kiosk) with one command.

## Options Considered

### Where does the C++ live?

#### Option A: C++ as an edge *client* device (CHOSEN)
| Dimension | Assessment |
|-----------|------------|
| Narrative coherence | High — attaches to the existing backend as a device |
| RPC-pattern coverage | Full — unary + client + bidi land naturally in a device lifecycle |
| CAN-FD / embedded fit | High — a kiosk has real actuators/sensors |
| Effort | Medium — new client + a few server RPCs |

**Pros:** One coherent device↔cloud story; all four streaming patterns map to concrete device actions; CAN-FD and store-and-forward have a believable home; reuses the C# core as system of record.
**Cons:** The C++ is "only" a client of the main flow (mitigated by the optional compute-server phase below).

#### Option B: Rewrite the core (or a core service) as a C++ gRPC server
**Pros:** Shows C++ on the server side; maximal C++ surface.
**Cons:** Throws away the Clean Architecture/CQRS/DDD investment that is itself a portfolio asset; large effort for a result that competes with, rather than complements, the existing work; breaks the "one coherent solution" property.

#### Option C: Standalone C++ repo, unrelated to this project
**Pros:** Freedom to design from scratch.
**Cons:** Two disjoint portfolios tell no single story; loses the cross-language-IDL demonstration entirely, which is the most valuable signal for this specific role.

### How "real" should CAN be?

#### Option A: Real `SocketCAN` `vcan0` + CAN-FD + bit-level codec (CHOSEN)
**Pros:** Genuine kernel CAN stack; CAN-FD 64-byte frames; survives entry-level scrutiny ("show me where you set `CAN_RAW_FD_FRAMES`"); runs hardware-free in CI.
**Cons:** Linux-only; slightly more setup (load `vcan` module, `ip link`); accepted and scripted.

#### Option B: A thread emitting fake hex frames
**Pros:** Trivial; cross-platform.
**Cons:** Demonstrates none of the actual CAN skill; the easiest thing for an interviewer to puncture. **Rejected** — it would undercut the very requirement it is meant to satisfy.

### Heavy compute on the device link

#### Option A: Defer a dedicated C++ compute *server* to an optional stretch phase (CHOSEN)
Keeps the core decision finishable; promotes C++ to the *server* side only once the client story is solid. See Implementation Plan, Phase 7.

#### Option B: Build the compute server up front
**Rejected for now** — risks scope creep before the differentiators (CAN-FD, bidi, resilience) are done. The plan explicitly sequences it last.

## Trade-off Analysis

The central trade-off is **C++ surface area vs. a finishable, coherent story.** Option B (C++ server) maximizes C++ but fragments the architecture and discards existing strengths; Option C maximizes freedom but tells two stories instead of one. Putting C++ at the edge concentrates effort on exactly the requirements the role grades hardest — heterogeneous bindings from one IDL, all four streaming patterns, CAN-FD, embedded delivery — while the C# core keeps doing what it already does well.

The one place we deliberately spend beyond the minimum is **real `SocketCAN` over a fake frame generator**, because that single choice is the difference between *claiming* and *demonstrating* CAN-FD experience — the headline "significant plus" for an eBike role. The store-and-forward / mTLS work is the second deliberate over-spend, because resilience and device identity are what make the result read as senior rather than as a tutorial.

## Scope

### In scope
- New `kiosk.proto` in `LibrarySystem.Contracts/Protos/`: `service Kiosk` with `ValidateMember` (unary), `BulkReturn` (client-stream), `SyncOfflineQueue` (client-stream), `DeviceLink` (bidi); telemetry + control message types with explicit field semantics.
- C++20 kiosk client under `clients/cpp/` (CMake + vcpkg/Conan for `grpc`, `protobuf`, `sqlite3`): channel/stub lifecycle (RAII, smart pointers), the four RPC call sites, `CanReader` (SocketCAN/CAN-FD), DBC-style signal codec, SQLite store-and-forward, mTLS.
- C# server handlers for the new `Kiosk` service in `LibrarySystem.Grpc`, wired through the existing MediatR/CQRS + Clean Architecture layers; idempotent ingestion for `SyncOfflineQueue`.
- A telemetry projection so the WPF Inspector renders live device gauges.
- Google Benchmark (C++) + BenchmarkDotNet (C#) for frame encode/decode and serialization.
- Static `aarch64` (musl) cross-compile of the kiosk; `docker compose` profile that boots the full fleet incl. `vcan0`.
- Central package management: pin `Google.Protobuf` / `Grpc.*` versions; document the C++ toolchain pins.
- README + this ADR updated; a short "CAN-FD signal map" table documented.

### Out of scope (explicitly)
- Physical CAN hardware, real card readers, or real motors (all simulated via `vcan0` + generators).
- Replacing any existing C# service with C++ (the core stays C#).
- A second UI; telemetry surfaces inside the existing Inspector only.
- Production device-provisioning/PKI (a self-signed dev CA is used for mTLS).
- Windows support for the kiosk (Linux/embedded target only; documented).
- The optional C++ compute server is **planned but gated** — see Phase 7; it is not required for the ADR to be considered delivered.

## Consequences

### Positive
- **Closes the C++ gap inside one coherent system** — device↔cloud, not two projects.
- **All four streaming patterns** demonstrated against real device actions, not contrived endpoints.
- **CAN-FD shown, not claimed** — real `SocketCAN`, 64-byte frames, bit-level codec.
- **Cross-platform bindings from a single IDL** — the headline role requirement, demonstrated literally.
- **Senior signals**: store-and-forward resilience, mTLS device identity, measured p99 latency.
- **Runnable in 30 seconds** via `docker compose`, hardware-free, CI-friendly.
- **Reuses existing assets** — Clean Arch core, WPF Inspector (ADR-002), WAF (ADR-001), and the `skewrun` cross-compile toolchain.

### Negative / Risks
- **Linux-only kiosk** — `SocketCAN` is a Linux kernel facility; Windows devs run it in the provided container (documented).
- **Build complexity** — C++ toolchain (vcpkg/Conan + protoc/gRPC plugin + CMake) and a virtual-CAN setup step are heavier than a pure .NET build; mitigated by scripting and a dev container.
- **Cross-language version drift** — protobuf/gRPC versions must stay aligned across C# and C++; mitigated by pinning and a documented compatibility matrix.
- **Scope discipline required** — the bidi + CAN + resilience trio is the value; the optional compute server (Phase 7) must not be started before they are done.
- **Idempotency correctness** — offline replay needs dedupe keys to avoid double-returns; called out as a first-class test target.

## Action Items
1. [ ] Add `kiosk.proto` (`ValidateMember`, `BulkReturn`, `SyncOfflineQueue`, `DeviceLink` + telemetry/control messages); wire `Grpc.Tools` codegen.
2. [ ] Implement the `Kiosk` service handlers in `LibrarySystem.Grpc` through MediatR/CQRS; add idempotent ingestion.
3. [ ] Scaffold `clients/cpp/` (CMake + vcpkg/Conan); generate C++ stubs from the shared proto; implement the unary `ValidateMember` end to end.
4. [ ] Implement `BulkReturn` (client-stream) and the `DeviceLink` (bidi) telemetry+control loop.
5. [ ] Implement `CanReader` over `SocketCAN`/`vcan0` (CAN-FD) + DBC-style signal codec + frame generator.
6. [ ] Implement SQLite store-and-forward + `SyncOfflineQueue` drain with idempotency keys.
7. [ ] Add mTLS (dev CA) on the device↔cloud channel.
8. [ ] Add the live telemetry view to the WPF Inspector via a server-streaming projection.
9. [ ] Add Google Benchmark (C++) + BenchmarkDotNet (C#) for encode/decode + serialization.
10. [ ] Static `aarch64` (musl) cross-compile; `docker compose` fleet profile incl. `vcan0`.
11. [ ] (Stretch) Add the C++ compute server (Phase 7) and route a heavy task to it from the core.
12. [ ] Update README + CAN-FD signal map; link this ADR.
