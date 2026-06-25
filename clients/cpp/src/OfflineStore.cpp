#include "OfflineStore.h"
#include <iostream>
#include <sys/stat.h>

OfflineStore::OfflineStore(const std::string& db_path, int max_rows, int ttl_days) : db_path_(db_path), max_rows_(max_rows), ttl_days_(ttl_days) {
    if (sqlite3_open_v2(db_path_.c_str(), &db_, SQLITE_OPEN_READWRITE | SQLITE_OPEN_CREATE, nullptr) != SQLITE_OK) {
        std::cerr << "[OfflineStore] Error opening SQLite database: " << sqlite3_errmsg(db_) << std::endl;
        db_ = nullptr;
    } else {
#ifdef __linux__
        chmod(db_path_.c_str(), 0600); // Lock down file permissions
#endif
        // Enable WAL mode for better concurrency and crash resilience
        sqlite3_exec(db_, "PRAGMA journal_mode=WAL;", nullptr, nullptr, nullptr);
        sqlite3_exec(db_, "PRAGMA synchronous=NORMAL;", nullptr, nullptr, nullptr);
        InitSchema();
    }
}

OfflineStore::~OfflineStore() {
    if (db_) {
        sqlite3_close(db_);
    }
}

void OfflineStore::InitSchema() {
    const char* sql = 
        "CREATE TABLE IF NOT EXISTS pending_events ("
        "  id INTEGER PRIMARY KEY AUTOINCREMENT,"
        "  idempotency_key TEXT UNIQUE,"
        "  payload BLOB NOT NULL,"
        "  created_at DATETIME DEFAULT CURRENT_TIMESTAMP"
        ");";
    
    char* err_msg = nullptr;
    if (sqlite3_exec(db_, sql, nullptr, nullptr, &err_msg) != SQLITE_OK) {
        std::cerr << "[OfflineStore] Failed to init schema: " << err_msg << std::endl;
        sqlite3_free(err_msg);
    }
}

bool OfflineStore::StoreEvent(const LibrarySystem::Contracts::Protos::BufferedEvent& event) {
    if (!db_) return false;

    sqlite3_stmt* count_stmt;
    if (sqlite3_prepare_v2(db_, "SELECT COUNT(*) FROM pending_events", -1, &count_stmt, nullptr) == SQLITE_OK) {
        if (sqlite3_step(count_stmt) == SQLITE_ROW) {
            int current_count = sqlite3_column_int(count_stmt, 0);
            if (current_count >= max_rows_) {
                std::cerr << "[OfflineStore] WARNING: Offline store full (" << current_count << " rows). Dropping event." << std::endl;
                sqlite3_finalize(count_stmt);
                return false;
            }
        }
        sqlite3_finalize(count_stmt);
    }

    std::string serialized;
    if (!event.SerializeToString(&serialized)) {
        std::cerr << "[OfflineStore] Failed to serialize event" << std::endl;
        return false;
    }

    const char* sql = "INSERT OR IGNORE INTO pending_events (idempotency_key, payload) VALUES (?, ?)";
    sqlite3_stmt* stmt;
    if (sqlite3_prepare_v2(db_, sql, -1, &stmt, nullptr) != SQLITE_OK) {
        std::cerr << "[OfflineStore] Prepare failed: " << sqlite3_errmsg(db_) << std::endl;
        return false;
    }

    std::string idempotency_key = event.idempotency_key();
    if (idempotency_key.empty()) {
        sqlite3_bind_null(stmt, 1);
    } else {
        sqlite3_bind_text(stmt, 1, idempotency_key.c_str(), -1, SQLITE_TRANSIENT);
    }
    sqlite3_bind_blob(stmt, 2, serialized.data(), serialized.size(), SQLITE_TRANSIENT);

    bool success = (sqlite3_step(stmt) == SQLITE_DONE);
    sqlite3_finalize(stmt);
    return success;
}

std::vector<BufferedItem> OfflineStore::GetPendingEvents(int limit) {
    std::vector<BufferedItem> items;
    if (!db_) return items;

    const char* sql = "SELECT id, payload FROM pending_events ORDER BY id ASC LIMIT ?";
    sqlite3_stmt* stmt;
    if (sqlite3_prepare_v2(db_, sql, -1, &stmt, nullptr) != SQLITE_OK) {
        return items;
    }

    sqlite3_bind_int(stmt, 1, limit);

    while (sqlite3_step(stmt) == SQLITE_ROW) {
        BufferedItem item;
        item.id = sqlite3_column_int64(stmt, 0);

        if (item.event.ParseFromArray(sqlite3_column_blob(stmt, 1), sqlite3_column_bytes(stmt, 1))) {
            items.push_back(item);
        }
    }

    sqlite3_finalize(stmt);
    return items;
}

bool OfflineStore::MarkAsSynced(const std::vector<int64_t>& ids) {
    if (!db_ || ids.empty()) return false;
    constexpr size_t kChunkSize = 100;
    bool all_ok = true;
    for (size_t i = 0; i < ids.size(); i += kChunkSize) {
        size_t end = std::min(i + kChunkSize, ids.size());
        std::string sql = "DELETE FROM pending_events WHERE id IN (";
        for (size_t j = i; j < end; ++j) sql += (j == i ? "?" : ",?");
        sql += ")";
        sqlite3_stmt* stmt;
        if (sqlite3_prepare_v2(db_, sql.c_str(), -1, &stmt, nullptr) != SQLITE_OK) { all_ok = false; continue; }
        for (size_t j = i; j < end; ++j) sqlite3_bind_int64(stmt, j - i + 1, ids[j]);
        if (sqlite3_step(stmt) != SQLITE_DONE) all_ok = false;
        sqlite3_finalize(stmt);
    }
    return all_ok;
}

void OfflineStore::SweepExpired() {
    if (!db_) return;
    const char* sql = "DELETE FROM pending_events WHERE created_at <= datetime('now', ?)";
    sqlite3_stmt* stmt;
    if (sqlite3_prepare_v2(db_, sql, -1, &stmt, nullptr) != SQLITE_OK) {
        std::cerr << "[OfflineStore] Failed to prepare sweep statement: " << sqlite3_errmsg(db_) << std::endl;
        return;
    }
    
    std::string modifier = "-" + std::to_string(ttl_days_) + " days";
    sqlite3_bind_text(stmt, 1, modifier.c_str(), -1, SQLITE_TRANSIENT);
    
    if (sqlite3_step(stmt) != SQLITE_DONE) {
        std::cerr << "[OfflineStore] Failed to sweep expired events: " << sqlite3_errmsg(db_) << std::endl;
    }
    sqlite3_finalize(stmt);
}
