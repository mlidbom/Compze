# CI Failures in Builds 767–768: Skip Bug Fix Fallout

## Summary

Builds 767 and 768 (both on `worktree_2`) are the first CI runs after fixing the matrix attribute skip bug. Previously, the bug caused `[Skip<SqlLayer>]` to incorrectly skip tests for **all** `SqlLayer` values instead of only the specified ones. Now that tests are correctly running for non-skipped combinations, real failures are surfacing.

**All failures are in a single test class:** `AccountManagement.PerformanceTest` in `AccountManagement.Tests.PerformanceInternals.dll`.

**Evidence of the skip bug:** In the previous successful build (764), the PgSql:Microsoft job reported `Passed: 1, Skipped: 3` for this assembly. After the fix in builds 767–768, it reports `Passed: 2–3, Failed: 1–2, Skipped: 0` — the three previously-skipped tests are now executing.

## Failing Tests

### 1. `Multithreaded_fetches_XX_account_resources_in_20_milliseconds`

| Build | Combinations that failed |
|-------|-------------------------|
| 767   | PgSql:SimpleInjector, PgSql:Autofac |
| 768   | PgSql:Microsoft, PgSql:SimpleInjector |

**Error:** `Npgsql.PostgresException (0x80004005): 40001: could not serialize access due to read/write dependencies among transactions`

**Root cause:** PostgreSQL serialization conflict under concurrent reads/writes. The exception originates in `PgSqlTeventStoreSqlLayer.Write.cs` (line 26) during `InsertSingleTaggregateTevents`. Multithreaded account registration triggers parallel event inserts that conflict at the PostgreSQL serializable isolation level.

### 2. `Multithreaded_logs_in_XX_times_in_100_milliseconds`

| Build | Combinations that failed |
|-------|-------------------------|
| 768   | PgSql:Microsoft |

**Error:** Same PostgreSQL serialization conflict (`40001: could not serialize access due to read/write dependencies among transactions`). This test creates accounts in a threaded setup phase and then does concurrent logins. The setup phase hits the same serialization conflict as test #1.

### 3. `Multithreaded_creates_XX_accounts_in_60_milliseconds__db2_memory__msSql__mySql__oracle_pgSql_`

| Build | Combinations that failed |
|-------|-------------------------|
| 767   | MsSql:Microsoft, MsSql:SimpleInjector, MsSql:Autofac |
| 768   | MsSql:Microsoft, MsSql:SimpleInjector, MsSql:Autofac |

**Error (MsSql):** `Microsoft.Data.SqlClient.SqlException (0x80131904): Transaction (Process ID XX) was deadlocked on lock resources with another process and has been chosen as the deadlock victim.`

**Error context:** The deadlock exception gets wrapped in `MessageDispatchingFailedException` → `AggregateException`. The `Register` command dispatched via HTTP to the AspNetCore-hosted endpoint hits a deadlock when multiple parallel registrations contend for the same lock resources.

**Note:** This test already has `[Skip<SqlLayer>([SqlLayer.Sqlite, SqlLayer.SqliteMemory], "SQLite deadlocks under parallel writes")]` — the same deadlock problem exists on MsSql and PgSql but was never observed because the tests were incorrectly skipped for those combinations too.

## Failure Categories

### Category A: PostgreSQL Serialization Conflicts (Tests #1 and #2)
- **Affects:** All PgSql combinations
- **Mechanism:** PostgreSQL's serializable isolation level rejects concurrent transactions that have read/write dependencies. The tevent store inserts events for different aggregates in parallel, but the serializable isolation level sees cross-transaction dependencies and aborts one with error code `40001`.
- **Typical fix:** Retry logic for serialization failures (PostgreSQL recommends retrying `40001` errors), or using a less strict isolation level where appropriate.

### Category B: SQL Server Deadlocks (Test #3)
- **Affects:** All MsSql combinations
- **Mechanism:** Classic SQL Server deadlock under concurrent writes. Multiple `Register` commands execute in parallel, each writing to the event store within a transaction. SQL Server detects a deadlock cycle and kills one transaction.
- **Performance context:** The `TimeAsserter.TimeOutException` in the error trace shows the test also exceeded its time budget (e.g., `Total:02.693 → 641% of maxTotal: 00.420`) because the deadlock recovery causes retries that blow the timing window.

## Affected Combinations Summary

| Build | PgSql:Microsoft | PgSql:SimpleInjector | PgSql:Autofac | MsSql:Microsoft | MsSql:SimpleInjector | MsSql:Autofac |
|-------|:-:|:-:|:-:|:-:|:-:|:-:|
| 767   | pass | FAIL (1) | FAIL (1) | FAIL (1) | FAIL (1) | FAIL (1) |
| 768   | FAIL (2) | FAIL (1) | pass | FAIL (1) | FAIL (1) | FAIL (1) |

(Numbers in parentheses = count of failed tests in that job)

MySql and Sqlite/SqliteMemory combinations are not affected — MySql combinations passed in both builds, and Sqlite/SqliteMemory are correctly skipped by the `[Skip]` attribute.

## Action Items

The concurrency issues (PgSql serialization failures, MsSql deadlocks) need to be addressed in the framework's persistence layer or the tests need to account for these known database behaviors:

1. **Option A — Add retry logic** for serialization failures (`40001` on PgSql) and deadlocks (`1205` on MsSql) in the SQL persistence layers.
2. **Option B — Extend the `[Skip]` attribute** to also skip PgSql and MsSql for these specific tests if the concurrency issues are known/accepted limitations.
3. **Option C — Adjust isolation levels** if the serializable isolation level is stricter than necessary for these operations.
