# MySql Test Failures — 2026-02-25

**Configuration**: `MySql:Microsoft:Newtonsoft:Memory`  
**Results**: 30 failed, 1008 passed, 15 skipped, 1053 total (64.4 seconds)

Three distinct bugs were uncovered, all silent under `SqliteMemory` testing.

---

## Bug 1: TeventStore Deletion Silently Fails (4 tests)

### Symptom
Deleting a taggregate from the tevent store has no effect — events remain in the database. Tests that delete and then assert the data is gone all fail.

### Root Cause
`MySqlTeventStoreSqlLayer.DeleteTaggregate` passes the `TaggregateId` wrapper object as the SQL parameter value instead of the unwrapped `Guid`:

```csharp
// MySqlTeventStoreSqlLayer.Write.cs line ~114
command.Parameters.Add(new MySqlParameter(Tevent.TaggregateId, MySqlDbType.Guid) 
    { Value = taggregateId });      // BUG: should be taggregateId.Value
```

The MySql connector can't match the `TaggregateId` wrapper against the stored GUID column, so `DELETE` silently affects 0 rows.

**The same bug exists in `MsSqlTeventStoreSqlLayer`** (SQL Server), which also passes the wrapper:

```csharp
// MsSqlTeventStoreSqlLayer.Write.cs line ~117
command.Parameters.Add(new SqlParameter(Tevent.TaggregateId, SqlDbType.UniqueIdentifier) 
    { Value = taggregateId });      // BUG: should be taggregateId.Value
```

SQLite and PostgreSQL are **not** affected — they correctly unwrap the value:
- SQLite: `AddVarcharParameter(Tevent.TaggregateId, 36, taggregateId.ToString())`
- PostgreSQL: `AddParameter(Tevent.TaggregateId, taggregateId.Value)`

### Fix
Change `Value = taggregateId` to `Value = taggregateId.Value` in both MySQL and MsSql implementations.

### Affected Tests
| Test Class | Test Method |
|---|---|
| `TeventStoreTests` | `DeleteTeventsDeletesTheTeventsForOnlyTheSpecifiedTaggregate` |
| `TeventStoreUpdaterTest` | `TaggregateCannotBeRetrievedAfterBeingDeleted` |
| `TeventStoreUpdaterTest` | `When_deleting_and_then_fetching_an_taggregates_history_the_history_should_be_gone` |
| `TeventStoreUpdaterTest` | `When_fetching_and_deleting_an_taggregate_then_fetching_history_again_the_history_should_be_gone` |

---

## Bug 2: UtcTimeStamp Precision Loss via MySQL DATETIME(6) (22 tests)

### Symptom
`DeepEqual` assertions fail on `UtcTimeStamp` fields. The last fractional digit is truncated:

```diff
- "UtcTimeStamp": "2026-02-25T07:01:42.5760679Z"   (7 digits — .NET)
+ "UtcTimeStamp": "2026-02-25T07:01:42.576067Z"     (6 digits — MySQL)
```

### Root Cause
MySQL's `DATETIME` type supports a **maximum** of microsecond precision (6 fractional digits). .NET `DateTime` has 100-nanosecond tick precision (7 fractional digits). The 7th digit is permanently lost on storage.

| Provider | Column Type | Parameter Type | Precision |
|---|---|---|---|
| **MySQL** | `datetime(6)` | `MySqlDbType.DateTime` | **6 digits (microseconds)** |
| SQLite | `INTEGER` (ticks) | `SqliteType.Integer` | 7 digits (100ns) |
| SQL Server | `datetime2(7)` | `SqlDbType.DateTime2` | 7 digits (100ns) |
| PostgreSQL | `timestamp` | `NpgsqlDbType.Timestamp` | 6 digits (microseconds) — **same issue likely** |

### Possible Fixes
1. **Store as BIGINT ticks** (like SQLite does) — preserves full precision but loses MySQL-native datetime querying
2. **Truncate timestamps to microseconds before storing** — accept 6-digit precision as the lowest common denominator across all providers
3. **Truncate in tests only** — adjust `DeepEqual` tolerance for timestamps (fragile, masks real issues)

Option 2 is recommended: truncate to microsecond precision at the framework level since that's the highest precision universally supported across all target databases.

### Affected Tests
All 22 `TeventMigrationTest` methods:

| Test Method |
|---|
| `Given_Ec1_E1_Ef_Inserting_E3_E4_before_E1_then_E5_before_E4` |
| `Given_Ec1_E1_Inserting_E2_before_E1_then_E3_before_E2` |
| `Given_Ec1_E1_Inserting_E3_E2_before_E1_then_E4_before_E3_then_E5_before_E4` |
| `Inserting_E2_after_E1` |
| `Inserting_E2_before_E1_then_E3_before_E2` |
| `Inserting_E2_Before_E1` |
| `Inserting_E3_before_E1` |
| `Inserting_E3_E4_before_E1_then_E5_before_E3_2` |
| `Inserting_E3_E4_before_E1_then_E5_before_E3` |
| `Inserting_E3_E4_before_E1_then_E5_before_E4_2` |
| `Inserting_E3_E4_before_E1_then_E5_before_E4_then_replace_E4_with_E6_then_replace_Ef_with_E7_then_insert_E8_after_E7` |
| `Inserting_E3_E4_before_E1` |
| `PersistingMigrationsOfTheSameTaggregateMultipleTimes` |
| `PersistingMigrationsOfTheSameTaggregateMultipleTimesWithTeventsAddedInTheMiddleAndAfter` |
| `Replacing_E1_with_E2_at_end_of_stream` |
| `Replacing_E1_with_E2_E3_2` |
| `Replacing_E1_with_E2_E3_then_an_unrelated_migration_v2` |
| `Replacing_E1_with_E2_E3_then_E2_with_E4` |
| `Replacing_E1_with_E2_E3` |
| `Replacing_E1_with_E2_then_irrelevant_migration` |
| `Replacing_E1_with_E2` |
| `UpdatingAnTaggregateAfterPersistingMigrations` |

---

## Bug 3: Performance Thresholds Calibrated for In-Memory Only (4 tests)

### Symptom
Performance tests exceed their `maxTotal` time thresholds by 2-3x:

| Test | Actual | Threshold | Ratio |
|---|---|---|---|
| `TeventMigrationPerformanceTest` (uncached, 4 migrations) | 133ms | 40ms | 334% |
| `DocumentDbPerformanceTests` (save XX docs) | 146ms | 50ms | 293% |
| `TeventMigrationPerformanceTest` (no migrations) | — | — | — |
| `AccountManagement.PerformanceTest` (create XX accounts) | — | — | — |

### Root Cause
Performance thresholds were tuned for `SqliteMemory` which has zero I/O latency. MySQL involves network round-trips and disk I/O, making operations inherently slower.

### Possible Fixes
1. **Scale thresholds by persistence layer** — multiply `maxTotal` by a factor when running against external databases
2. **Use `COMPOSABLE_MACHINE_SLOWNESS` env var** — the existing mechanism for slow machines may help, but the gap is structural (in-memory vs network DB), not just machine speed
3. **Separate performance thresholds per provider** — define different `maxTotal` values depending on whether it's an in-memory or external database

---

## Summary

| Bug | Severity | Tests | Providers Affected | Fix Complexity |
|---|---|---|---|---|
| **1. Deletion silent failure** | **Critical** | 4 | MySQL, MsSql | Trivial (`.Value`) |
| **2. Timestamp precision loss** | **Medium** | 22 | MySQL (possibly PostgreSQL) | Small–Medium |
| **3. Performance thresholds** | **Low** | 4 | All external DBs | Design decision |

**Total**: 30 failures = 4 (deletion) + 22 (timestamps) + 4 (performance)
