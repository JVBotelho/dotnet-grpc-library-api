# C++ / C# Compatibility Matrix

This file tracks the versions of gRPC and Protobuf used in the C# `.NET 10` ecosystem and their corresponding `vcpkg` ports to prevent version drift.

The `vcpkg.json` `builtin-baseline` is pinned to ensure we pull matching versions.

## Current Pins (Phase 5)

| Tool / Library | C# (`Directory.Packages.props`) | C++ (`vcpkg` baseline: `2024.02.14`) |
|----------------|----------------------------------|-----------------------------------|
| **Protobuf**   | `3.33.5`                         | `~3.x`                            |
| **gRPC**       | `2.76.0`                         | `~1.6x`                           |

*Note: C++ gRPC versions often align differently than the .NET wrapper versions (1.x vs 2.x), but the wire format and `protoc` generation remain completely compatible. The key is preventing drift within each ecosystem's established lockfile. For C++ Compute and Kiosk clients, we pin vcpkg to `2024.02.14` tag to guarantee deterministic compilation and linkability.*
