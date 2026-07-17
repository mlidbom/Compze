# Changelog

All notable changes to Compze.Tessaging.Sqlite will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.2.0-alpha

- The tessaging SQL layer implementations are async end to end (`SaveTessageAsync`, `MarkAsReceivedAsync`, `GetUndeliveredTessagesForEndpointAsync`, the inbox marks, the peer-registry members — matching the now-async `ITessagingSqlLayer`): exactly-once tessaging is database I/O, and synchrony-follows-the-type reaches the storage first. Same SQL, async command and reader plumbing.
- Adding exactly-once Tessaging on a sqlite foundation also wires the type-id interner, derived from the foundation's declaration (`SqliteTypeIdInterner(SqliteEndpointDatabase)`) — on sqlite the interner has its own database, so the registrar-level `SqliteTessagingSqlLayer()` cannot demand it namelessly; registrar-level compositions register it explicitly.
- `SqliteEndpointDatabase(connectionStringName)` on `ExactlyOnceEndpointBuilder`: declares that the endpoint's database is sqlite, filling the exactly-once endpoint's one database parameter with the whole engine pairing — the connection pool, the sqlite type-id interner Tessaging's sql layers share (derived from the declaration), and Tessaging's sqlite inbox/outbox sql layers.

## 0.1.0-alpha

- Initial pre-release
