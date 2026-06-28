# CI-only triplet: release builds only, skipping debug.
# gRPC debug binaries with DWARF symbols exhaust the 7 GB RAM on ubuntu-latest.
# VCPKG_BUILD_TYPE=release skips the x64-linux-dbg step for every package,
# cutting compilation time and peak memory roughly in half.
set(VCPKG_TARGET_ARCHITECTURE x64)
set(VCPKG_CRT_LINKAGE dynamic)
set(VCPKG_LIBRARY_LINKAGE static)
set(VCPKG_CMAKE_SYSTEM_NAME Linux)
set(VCPKG_BUILD_TYPE release)
