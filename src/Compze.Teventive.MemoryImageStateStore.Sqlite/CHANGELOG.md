# Changelog

All notable changes to Compze.Teventive.MemoryImageStateStore.Sqlite will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.1.0-alpha

- **The SQLite tables of the memory image state store:** `SqliteMemoryImageStateStoreSqlLayer()`, declared beside the domain database, contributes the store's tables to its schema and registers their SQL, for a store composed with `StoredInTheDomainDatabase(...)`.
