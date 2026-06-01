# Split the SQL engine projects by feature — DONE

Each `Compze.Internals.Sql.<Engine>` monolith was split into 5 projects per engine (Sqlite, MySql, PostgreSql, MicrosoftSql):

| Project | Role |
|---|---|
| `Compze.Internals.Sql.<Engine>` *(kept name)* | Plumbing: connection, pool, command extensions, exception mapping. **Core-free** (references `Sql.Common` + `Compze.Abstractions` + ADO driver). `[InternalsVisibleTo]` the 4 feature backends so they can use the internal command extensions / `SqlExceptions`. |
| `Compze.DocumentDb.<Engine>` | `IDocumentDbSqlLayer` impl — Core-free. |
| `Compze.TypeIdentifiers.Interning.<Engine>` | `ITypeIdInternerPersistence` impl — Core-free. |
| `Compze.Tessaging.<Engine>` | inbox/outbox `IServiceBusSqlLayer` impl — pulls Core (interfaces live in `Compze.Core`). |
| `Compze.Tessaging.Teventive.TeventStore.<Engine>` | TeventStore SQL layer — pulls Core. |

Engine tokens for project names: `Sqlite` / `MySql` / `PostgreSql` / `MicrosoftSql`. Source type prefixes unchanged: `Sqlite` / `MySql` / `PgSql` / `MsSql`.

**Umbrella wiring:** no meta-package. The composition root (`Compze.Tessaging.Hosting.Testing`) references each backend and chains the feature registrars itself.

## Schema initialization — kept behavior, did NOT dissolve

The original plan said to dissolve `<Engine>SqlLayerSchemaManager` into per-backend self-init. That changed behavior and broke file-SQLite: per-backend lazy `CREATE TABLE` (on a suppressed connection) can run while a business transaction already holds the file's read lock (SQLite readers block writers) → `SQLITE_BUSY`. The original created **all** tables together on the first touch, before any lock.

So the schema manager was **kept** (one suppressed `RunOnceAsync` batch on first touch = identical behavior), but **decoupled**: it now lives in the plumbing project taking the SQL scripts via its constructor (`IReadOnlyList<string>`) instead of hard-referencing the feature layers. Each feature backend exposes its `SchemaCreationSql` on its public registrar; the composition root passes the present engine's scripts to `<Engine>SqlLayerSchemaManager(...)`. Plumbing still references no feature backend, so single-feature consumers keep clean closures. (Compze's DI is single-instance / explicitly-typed — no `IEnumerable<T>` multi-binding — so scripts are assembled by the composition root rather than collected via DI.)

## Verification

`C-Build` clean; `C-Test -NoBuild -SingleThreadedTesting` → **3703 tests, 0 failures** (baseline). The previously-regressing file-SQLite performance test passes.

## Out of scope (not done here)

- Splitting `Compze.Core`; moving the Tessaging/TeventStore SQL-layer interfaces out of Core.
- Core hygiene fix "B" (relocating `DocumentDbRegistrar`/`IEndpointHost` out of Core).
- Sweep of now-unused `using` directives left by the moves (IDE0005/CS8019 hints only — not build warnings) via `jb inspectcode` / `/simplify`.
