# Changelog

All notable changes to Compze.DbPool will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.4.0-alpha

- **A pool's calls for different reservation names no longer wait for each other's reservation and reset:** `DbPool.ConnectionStringFor` held the pool's lock while it reserved a database, which can wait 45 seconds for a free one, and while it reset it, which can take seconds on a busy server or wait for a lock someone holds on the database. The lock gave up after 30 seconds, so a call for another name, such as another endpoint of the same host starting at the same time, threw a lock timeout. Now the lock is held only to record the reservation; calls for the same name wait until its database is ready, and `Dispose` waits for the reservations still in progress. A database whose reset failed is reset again by the next call for its name, instead of being handed out as it was.
- **A pool that held a database past the end of its reservation throws when disposed,** naming each such database and how long it held it. A reservation lasts 65 seconds; once it has ended, another pool may reserve the database and reset it while it is still in use. That failed the database's users in ways that pointed elsewhere, such as an unknown database or a missing table, and only when another pool happened to take it. Now the overrun fails every time, at its source.
- **The reservations of a process that has exited are released at the next reservation, by any pool on the machine:** each reservation records the process it was made in, so a database a killed or crashed process held is free at once, not 65 seconds later.
- **The pool databases are named `Compze_DbPool_v3_0001` and on, and their reservations are kept apart from those of earlier versions:** an earlier version can't read the reservations this one keeps, and would replace them with none. So pools of the two versions, as in two checkouts of different ages on one machine, never share a database.
- **A SQL layer says how many databases the pools on a machine may create with it: `IDbPoolSqlLayer.MaximumNumberOfDatabases`,** 100 unless the layer says otherwise, as every layer allowed before. Each database the pools create stays, for reuse, so on a database server each one takes disk space for good; Compze.DbPool.Sqlite's in-memory layer allows many more.
- **A reservation that gives up waiting for a free database says why:** how many of the databases were reserved, by how many pools in how many processes, and that a pool waiting for a database keeps those it holds. So when pools that each wait for one more hold every database, none is released until their waits time out, however many databases there are: more tests held databases at once than there were databases. It said only "Have you missed disposing a database pool?", which sent the reader looking for a leak.
- **`DbPoolState.TryReserve` is internal:** only the pool reserves its databases.
- **Requires newer minor versions of other Compze packages:** Compze.DependencyInjection 0.11.0-alpha (was 0.10.0-alpha), Compze.Internals.SystemCE 0.14.0-internal (was 0.12.0-internal), Compze.Threading.Interprocess 0.11.1-alpha (was 0.9.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.3.1-alpha

- **Depends on Compze.Threading.Interprocess instead of Compze.InterprocessObject, which is retired:** `IInterprocessObject<T>`, which it keeps its machine-wide state in, moved to Compze.Threading.Interprocess. Nothing public changes.
- **Requires newer minor versions of other Compze packages:** Compze.DependencyInjection 0.10.0-alpha (was 0.9.0-alpha), Compze.Internals.SystemCE 0.12.0-internal (was 0.8.0-internal), Compze.Threading 0.14.0-alpha (was 0.12.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.3.0-alpha

- **Targets .NET 11 (`net11.0`) instead of .NET 10, and is compiled with runtime async:** the runtime, instead of state machines the compiler generates, suspends and resumes its async methods, so live stack traces show the real call chain. An application that uses this version must target .NET 11.
- **Requires newer minor versions of other Compze packages:** Compze.DependencyInjection 0.9.0-alpha (was 0.8.0-alpha), Compze.Internals.Logging 0.7.0-alpha (was 0.4.1-alpha), Compze.Internals.SystemCE 0.8.0-internal (was 0.4.0-internal), Compze.InterprocessObject 0.9.0-alpha (was 0.7.1-alpha), Compze.Threading 0.12.0-alpha (was 0.9.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.2.2-alpha

- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.2.1-alpha

- Refactoring.
- Version aligned with Compze.Internals.Sql.* 0.2.1-alpha.

## 0.1.0-alpha.3

- Initial pre-release
