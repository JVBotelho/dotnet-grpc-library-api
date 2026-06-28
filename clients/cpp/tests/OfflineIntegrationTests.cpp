#include <gtest/gtest.h>
#include "OfflineStore.h"
#include "kiosk.pb.h"
#include <fstream>
#include <cstdio>
#include <string>

using namespace LibrarySystem::Contracts::Protos;

class OfflineIntegrationTests : public ::testing::Test {
protected:
    void SetUp() override {
        db_path = "test_offline.db";
        std::remove(db_path.c_str());
    }

    void TearDown() override {
        std::remove(db_path.c_str());
    }

    std::string db_path;
};

TEST_F(OfflineIntegrationTests, StoreAndRetrieveEvent) {
    OfflineStore store(db_path, 100, 7);

    BufferedEvent event;
    event.set_idempotency_key("test_key_1");

    ReturnScan* scan = new ReturnScan();
    scan->set_book_id(123);
    event.set_allocated_return_scan(scan);

    EXPECT_TRUE(store.StoreEvent(event));

    auto pending = store.GetPendingEvents(10);
    EXPECT_EQ(pending.size(), 1);
    EXPECT_EQ(pending[0].event.idempotency_key(), "test_key_1");
    EXPECT_EQ(pending[0].event.return_scan().book_id(), 123);
}

TEST_F(OfflineIntegrationTests, DeduplicationByIdempotencyKey) {
    OfflineStore store(db_path, 100, 7);

    BufferedEvent event1;
    event1.set_idempotency_key("duplicate_key");
    event1.mutable_return_scan()->set_book_id(1);

    BufferedEvent event2;
    event2.set_idempotency_key("duplicate_key");
    event2.mutable_frame()->set_can_id(100);

    EXPECT_TRUE(store.StoreEvent(event1));
    EXPECT_TRUE(store.StoreEvent(event2)); // Should succeed but internally ignore due to INSERT OR IGNORE

    auto pending = store.GetPendingEvents(10);
    EXPECT_EQ(pending.size(), 1);
    EXPECT_TRUE(pending[0].event.has_return_scan());
}

TEST_F(OfflineIntegrationTests, MarkAsSyncedAndSweep) {
    OfflineStore store(db_path, 100, 0); // 0 days TTL so we can sweep immediately

    BufferedEvent event;
    event.mutable_return_scan()->set_book_id(10);
    event.set_idempotency_key("key1");
    EXPECT_TRUE(store.StoreEvent(event));

    event.set_idempotency_key("key2");
    EXPECT_TRUE(store.StoreEvent(event));

    auto pending = store.GetPendingEvents(10);
    EXPECT_EQ(pending.size(), 2);

    std::vector<int64_t> to_sync = { pending[0].id };
    EXPECT_TRUE(store.MarkAsSynced(to_sync));

    pending = store.GetPendingEvents(10);
    EXPECT_EQ(pending.size(), 1);
    EXPECT_EQ(pending[0].event.idempotency_key(), "key2");

    // Sweep should delete the rest because TTL is 0
    store.SweepExpired();
    pending = store.GetPendingEvents(10);
    EXPECT_EQ(pending.size(), 0);
}
