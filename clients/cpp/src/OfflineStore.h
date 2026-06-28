#pragma once
#include <string>
#include <vector>
#include <sqlite3.h>
#include "kiosk.pb.h"

struct BufferedItem {
    int64_t id;
    LibrarySystem::Contracts::Protos::BufferedEvent event;
};

class OfflineStore {
public:
    explicit OfflineStore(const std::string& db_path, int max_rows = 10000, int ttl_days = 7);
    ~OfflineStore();

    bool StoreEvent(const LibrarySystem::Contracts::Protos::BufferedEvent& event);
    std::vector<BufferedItem> GetPendingEvents(int limit = 100);
    bool MarkAsSynced(const std::vector<int64_t>& ids);
    void SweepExpired();

private:
    void InitSchema();
    sqlite3* db_{nullptr};
    std::string db_path_;
    int max_rows_;
    int ttl_days_;
};
