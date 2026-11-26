# Changelog

All notable changes to Compze.Teventive.MemoryImageStateStore.Sqlite will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.1.0-alpha

- **The tables of one memory image:** `MemoryImageStateStoreTaggregate` has no `CommitNumber` column and its `Snapshot` is never null, and `MemoryImageStateStoreCommitCounter` is gone, now that one memory image at a time uses them: see Compze.Teventive.MemoryImageStateStore's changelog. A database whose tables an earlier version created needs them dropped before this version first uses it.
- **The SQLite tables of the memory image state store:** `SqliteMemoryImageStateStoreSqlLayer()`, declared beside the domain database, contributes the store's tables to its schema and registers their SQL, for a store composed with `StoredInTheDomainDatabase(...)`.
