#include <gtest/gtest.h>
#include "OfflineStore.h"
#include <filesystem>

class OfflineStoreTest : public ::testing::Test {
protected:
    void SetUp() override {
        // Ensure a clean database before each test
        std::filesystem::remove("test_offline.db");
        store_ = std::make_unique<OfflineStore>("test_offline.db");
    }

    void TearDown() override {
        store_.reset();
        std::filesystem::remove("test_offline.db");
    }

    std::unique_ptr<OfflineStore> store_;
};

TEST_F(OfflineStoreTest, StoreAndRetrieveEvents) {
    LibrarySystem::Contracts::Protos::BufferedEvent event1;
    event1.set_idempotency_key("key-1");
    event1.mutable_frame()->set_can_id(123);

    LibrarySystem::Contracts::Protos::BufferedEvent event2;
    event2.set_idempotency_key("key-2");
    event2.mutable_frame()->set_can_id(456);

    EXPECT_TRUE(store_->StoreEvent(event1));
    EXPECT_TRUE(store_->StoreEvent(event2));

    auto pending = store_->GetPendingEvents(10);
    ASSERT_EQ(pending.size(), 2);
    
    EXPECT_EQ(pending[0].event.idempotency_key(), "key-1");
    EXPECT_EQ(pending[1].event.idempotency_key(), "key-2");
}

TEST_F(OfflineStoreTest, MarkAsSyncedRemovesEvents) {
    LibrarySystem::Contracts::Protos::BufferedEvent event;
    event.set_idempotency_key("key-1");
    store_->StoreEvent(event);

    auto pending = store_->GetPendingEvents(10);
    ASSERT_EQ(pending.size(), 1);

    int64_t id = pending[0].id;
    std::vector<int64_t> synced_ids = { id };
    
    EXPECT_TRUE(store_->MarkAsSynced(synced_ids));

    auto remaining = store_->GetPendingEvents(10);
    EXPECT_TRUE(remaining.empty());
}
