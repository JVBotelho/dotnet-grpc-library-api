#include <benchmark/benchmark.h>
#include "OfflineStore.h"
#include "kiosk.pb.h"
#include <filesystem>

using namespace LibrarySystem::Contracts::Protos;

static void BM_OfflineStore_Enqueue(benchmark::State& state) {
    std::filesystem::remove("bench_kiosk_offline.db");
    OfflineStore store("bench_kiosk_offline.db");

    BufferedEvent event;
    event.set_idempotency_key("bench-key");
    auto scan = event.mutable_return_scan();
    scan->set_device_id("BENCH-001");
    scan->set_book_id(1001);

    for (auto _ : state) {
        store.EnqueueEvent(event);
    }
    
    std::filesystem::remove("bench_kiosk_offline.db");
}
BENCHMARK(BM_OfflineStore_Enqueue);

static void BM_OfflineStore_Dequeue(benchmark::State& state) {
    std::filesystem::remove("bench_kiosk_offline.db");
    OfflineStore store("bench_kiosk_offline.db");

    // Pre-populate
    BufferedEvent event;
    for (int i = 0; i < state.range(0); ++i) {
        event.set_idempotency_key("key-" + std::to_string(i));
        store.EnqueueEvent(event);
    }

    for (auto _ : state) {
        auto batch = store.GetPendingEvents(50);
        benchmark::DoNotOptimize(batch);
        
        // Pause timing to not count the repopulate phase if we exhaust it
        if (batch.empty()) {
            state.PauseTiming();
            for (int i = 0; i < state.range(0); ++i) {
                event.set_idempotency_key("key-" + std::to_string(i));
                store.EnqueueEvent(event);
            }
            state.ResumeTiming();
        }
    }
    
    std::filesystem::remove("bench_kiosk_offline.db");
}
BENCHMARK(BM_OfflineStore_Dequeue)->Range(100, 10000);
