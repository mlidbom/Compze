# linq2db Evaluation for Compze SQL Layer

## Current State

The SQL layer uses **pure ADO.NET** with raw SQL string literals, duplicated across 4 provider projects:

| Project | Driver |
|---------|--------|
| `Compze.Sql.MicrosoftSql` | `Microsoft.Data.SqlClient` |
| `Compze.Sql.MySql` | `MySql.Data` |
| `Compze.Sql.PostgreSql` | `Npgsql` |
| `Compze.Sql.Sqlite` | `Microsoft.Data.Sqlite` |

Each functional area (DocumentDb, Inbox, Outbox, TeventStore, DbPool, Schema) is **fully reimplemented per provider**. There is no query builder, no dialect abstraction — every SQL string is hand-written per engine.

### Key Numbers

- **~48 `.cs` files** across the 4 SQL provider projects implementing the same 4 functional areas
- **~140 distinct raw SQL statement locations**
- **~3,000–4,000 lines** of duplicated SQL layer code

## Where linq2db Would Help

### 1. DocumentDb — Highest Impact

The 4 implementations are **~95% identical**. The only differences are:

| Variation | MsSql | MySql | PgSql | Sqlite |
|-----------|-------|-------|-------|--------|
| String param helper | `AddNVarcharParameter` | `AddVarcharParameter` | `AddVarcharParameter` | `AddVarcharParameter` |
| Text param helper | `AddNVarcharMaxParameter` | `AddMediumTextParameter` | `AddMediumTextParameter` | `AddMediumTextParameter` |
| DateTime param | `AddDateTime2Parameter` | `AddDateTime2Parameter` | `AddTimestampWithTimeZone` | `AddDateTime2Parameter` |
| GUID storage | native `uniqueidentifier` | native GUID | native UUID | `varchar(36)` string |
| GUID reading | `reader.GetGuid(n)` | `reader.GetGuid(n)` | `reader.GetGuid(n)` | `Guid.Parse(reader.GetString(n))` |
| Update lock hint | `With(UPDLOCK, ROWLOCK)` | *(none)* | *(none)* | *(none)* |
| PK exception type | `SqlException` | `MySqlException` | `PostgresException` | `SqliteException` |
| Prepare statement | No | No | Yes | No |

With linq2db: **1 implementation** instead of 4, dialect differences handled automatically.

### 2. SQL Injection-Adjacent Patterns — Safety Improvement

Current `TypeInClause` builds `IN(...)` via string-joining:

```csharp
"IN( '" + acceptableTypeIds.Select(guid => guid.ToString()).Join("', '") + "')"
```

`UpdateEffectiveVersions` interpolates values directly into SQL:

```csharp
$"UPDATE ... SET EffectiveVersion = {spec.EffectiveVersion} WHERE TeventId = '{spec.TeventId}'"
```

While these use `Guid`/`int` types (injection unlikely), the pattern is unsafe. linq2db parameterizes automatically.

### 3. Inbox/Outbox — Same Pattern as DocumentDb

4 copies of largely identical CRUD with minor dialect tweaks. Would collapse to single implementations.

### 4. Schema DDL — Moderate Impact

`CreateTable<T>()` handles type mapping (`uniqueidentifier` → `UUID` → `TEXT`) and `IF NOT EXISTS` automatically. Would replace 4 separate Schema partial classes per functional area.

## Where It Wouldn't Help Much

### 1. TeventStore ReadOrder — Structural Data Model Divergence

| Provider | ReadOrder Storage |
|----------|------------------|
| SQL Server | Single `decimal(38,19)` column, read via `GetSqlDecimal` |
| MySQL | Single `decimal(38,19)` column, read as `string` with locale fix |
| PostgreSQL | Single `numeric(38,19)` column, read as `string` with locale fix |
| SQLite | **Two INTEGER columns** (IntegerPart + FractionPart) |

This isn't a dialect difference — the *data model itself* diverges. linq2db can't abstract this away; provider-specific mapping logic would still be required.

### 2. Complex Lock Hints

- SQL Server: `WITH(UPDLOCK, READCOMMITTED, ROWLOCK)` table hints
- PostgreSQL: `FOR UPDATE` + separate `TaggregateLock` table
- MySQL/SQLite: No locking or partially commented out

linq2db has `TableHint`/`SubQueryHint` extensions, but they're somewhat clunky and not all hints are supported.

### 3. Provider-Specific SQL Constructs

- SQL Server's `MERGE...WHEN NOT MATCHED THEN INSERT` (Inbox)
- PostgreSQL's lock table INSERT for TeventStore
- Conditional `IF(@ReadOrder = 0) BEGIN...END` blocks (SQL Server) vs `WHERE` clause alternatives

These would still need raw SQL fragments or provider-specific branches.

### 4. Connection Pool Integration

The codebase has a custom `IDbConnectionPool<TConnection, TCommand>` abstraction. linq2db has its own `DataConnection` model. These can be reconciled by passing the already-pooled `DbConnection` to linq2db's `DataConnection(DataOptions, DbConnection)` constructor.

## Estimated Impact

| Area | Files Today | With linq2db | Reduction |
|------|------------|-------------|-----------|
| DocumentDb | 8 | 2 | ~75% |
| Inbox | 8 | 2–3 | ~65% |
| Outbox | 8 | 2–3 | ~65% |
| TeventStore | 12 | 6–8 | ~40% |
| Schema DDL | 8 | 2–3 | ~70% |
| DbPool | 4 | 3–4 | ~15% |
| **Total** | **~48** | **~18–23** | **~55%** |

Rough estimate: **~3,000–4,000 lines eliminated**, with remaining provider-specific code concentrated in TeventStore ReadOrder handling and lock strategies.

## Performance Considerations

linq2db generates SQL and maps results — no change tracking, no identity maps, no lazy loading. Its overhead vs raw ADO.NET is negligible. This aligns with the codebase's existing performance discipline.

## Risks

- **New transitive dependency** — each `Compze.Sql.*` project would depend on `linq2db`
- **TeventStore ReadOrder divergence** limits unification in the most complex area
- **Migration effort** — ~140 SQL statement locations to convert
- **Lock/hint handling** may still require per-provider code for advanced scenarios

## Recommendation

**Worth pursuing.** Start with a proof-of-concept branch converting DocumentDb only:

1. Add `linq2db` NuGet reference to one provider project (e.g., `Compze.Sql.Sqlite`)
2. Rewrite `SqliteDocumentDbSqlLayer` using linq2db
3. Verify tests pass unchanged
4. If successful, create a single dialect-agnostic `DocumentDbSqlLayer` and remove the 4 provider-specific copies
5. Expand to Inbox/Outbox, then tackle TeventStore last

This validates the approach with the lowest risk (DocumentDb is the simplest area with the most duplication).
