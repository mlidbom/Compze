# Changelog

All notable changes to Compze.Teventive.MemoryImageStateStore.PostgreSql will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.2.0-alpha

- **Registers the domain database's process locks,** which the store takes for its tables, and contributes their `DatabaseProcessLockHolder` table to the database's schema: see Compze.Teventive.MemoryImageStateStore's changelog.
- **The tables of one memory image:** `MemoryImageStateStoreTaggregate` has no `CommitNumber` column and its `Snapshot` is never null, and `MemoryImageStateStoreCommitCounter` is gone, now that one memory image at a time uses them: see Compze.Teventive.MemoryImageStateStore's changelog. A database whose tables an earlier version created needs them dropped before this version first uses it.
- **Requires newer minor versions of other Compze packages:** Compze.Teventive.MemoryImageStateStore 0.3.0-alpha (was 0.2.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.1.0-alpha

- **The PostgreSQL tables of the memory image state store:** `PgSqlMemoryImageStateStoreSqlLayer()`, declared beside the domain database, contributes the store's tables to its schema and registers their SQL, for a store composed with `StoredInTheDomainDatabase(...)`.
