# BDD-Style Test Migration Plan

Track the migration of flat-style tests to BDD-style nested specification classes per [csharp-bdd-specifications.md](../../.claude-shared/rules/path-scoped/csharp-bdd-specifications.md).

## Current State Summary

| Category | File count | Test count (approx) |
|----------|-----------|---------------------|
| Already BDD-style nested | ~25 files | ~400+ |
| Flat `[XF]` — migration candidates | ~30 files | ~120 |
| `[PCT]` integration/perf — harder migration | ~20 files | ~130 |
| Support/domain files (no tests) | ~25 files | — |

---

## Tier 1 — Easy Migrations (flat `[XF]`, obvious groupings, no shared state complexity)

These are the best starting points. Each is a small, self-contained file with straightforward flat tests that map naturally to BDD nested classes.

### Compze.Tests.Unit

| # | File | Tests | Suggested BDD structure |
|---|------|-------|------------------------|
| 1 | [PersistentEntityTests.cs](../test/Compze.Tests.Unit/DDD/PersistentEntityTests.cs) | 5 | `Given_two_Person_entities` → `with_same_id` / `with_different_ids` / `compared_to_null` |
| 2 | [TaggregateTests.cs](../test/Compze.Tests.Unit/CQRS/Taggregates/TaggregateTests.cs) | 3 | `Given_a_taggregate` → `after_raising_tevent` / `after_committing` / `when_observing_cascading_tevents` |
| 3 | [PublicSettersAndFieldsAreDisallowedTests.cs](../test/Compze.Tests.Unit/CQRS/Taggregates/PublicSettersAndFieldsAreDisallowedTests.cs) | 5 | `When_creating_taggregate_type` → `with_public_setters` / `with_public_fields` (throws) vs `with_allowed_public_setters` (succeeds) |
| 4 | [DummyTimeSourceTests.cs](../test/Compze.Tests.Unit/GenericAbstractions/Time/DummyTimeSourceTests.cs) | 3 | `Given_a_DummyTimeSource` → `UtcNow_returns_close_to_real_time` / `FrozenAtUtc_returns_exact_value` / `FrozenAtUtc_parses_string` |
| 5 | [DateTimeNowTimeSourceTests.cs](../test/Compze.Tests.Unit/GenericAbstractions/Time/DateTimeNowTimeSourceTests.cs) | 1 | `Given_a_DateTimeNowTimeSource` → `UtcNow_returns_close_to_real_time` |
| 6 | [IStaticInstancePropertySingleton_tests.cs](../test/Compze.Tests.Unit/GenericAbstractions/IStaticInstancePropertySingleton_tests.cs) | 2 | `Given_a_type_implementing_IStaticInstancePropertySingleton` → `implicit_implementation` / `explicit_implementation` |
| 7 | [Given_a_cat_taggregate_inheriting_from_an_animal_taggregate.cs](../test/Compze.Tests.Unit/CQRS/Taggregates/InheritingTaggregate/Given_a_cat_taggregate_inheriting_from_an_animal_taggregate.cs) | 2 | Already BDD-named. Add nested classes: `registering_birth` → `creates_cat` / `creates_dog` |

### Compze.Tests.Unit.Internals

| # | File | Tests | Suggested BDD structure |
|---|------|-------|------------------------|
| 8 | [ReadOrderTests.cs](../test/Compze.Tests.Unit.Internals/Sql/TeventStore/ReadOrderTests.cs) | 9 | `ReadOrder_specification` → `Parsing` (roundtrip, negatives, decimal validation) / `SqlDecimal_roundtripping` / `InsertionIntervals` / `CreateOrdersForTeventsBetween` (various gap sizes) |
| 9 | [EnumerableCE_specification.cs](../test/Compze.Tests.Unit.Internals/Linq/EnumerableCE_specification.cs) | 6 | `EnumerableCE_specification` → `Until` / `Through` / `By` (each with their boundary/count tests) |
| 10 | [LinqExtensionsTests.cs](../test/Compze.Tests.Unit.Internals/Linq/LinqExtensionsTests.cs) | 3 | `LinqExtensions_specification` → `Flatten` / `ChopIntoSizesOf` |
| 11 | [DocumentDBSession.DocumentKeyTests.cs](../test/Compze.Tests.Unit.Internals/KeyValueStorage/DocumentDBSession.DocumentKeyTests.cs) | 7 | `DocumentKey_specification` → `same_type_same_id` → equal / `same_type_different_id` → not equal / `inheriting_types` → not equal / `case_insensitive` / `trailing_spaces` |
| 12 | [AppConfigConfigurationParameterProviderTests.cs](../test/Compze.Tests.Unit.Internals/SystemCE/ConfigurationCE/AppConfigConfigurationParameterProviderTests.cs) | 3 | `Given_an_AppSettingsJsonConfigurationParameterProvider` → `with_existing_key` (returns value) / `with_missing_key` (throws, message contains key) |
| 13 | [MonitorClassAPIExploration.cs](../test/Compze.Tests.Unit.Internals/SystemCE/ThreadingCE/MonitorClassAPIExploration.cs) | 3 | `Monitor_Wait_behavior` → `returns_after_timeout_without_pulse` / `does_not_return_until_lock_reacquired` / `does_not_hang_on_long_timeout` |
| 14 | [NotDefault_method.cs](../test/Compze.Tests.Unit.Internals/Contracts/NotDefault_method.cs) | 1 | Already well-structured — small, focused file per method. Folder/namespace provides grouping. No migration needed. |
| 15 | [NotNull_method_throws_for.cs](../test/Compze.Tests.Unit.Internals/Contracts/NotNull_method_throws_for.cs) | 2 | Same — already good. |
| 16 | [NotNullOrDefault_method_throws_for.cs](../test/Compze.Tests.Unit.Internals/Contracts/NotNullOrDefault_method_throws_for.cs) | 1 | Same — already good. |
| 17 | [SeqTests.cs](../test/Compze.Tests.Unit.Internals/Linq/SeqTests.cs) | 1 | Single test — wrap in nested class |
| 18 | [ObjectExtensionsTest.cs](../test/Compze.Tests.Unit.Internals/ObjectExtensionsTest.cs) | 1 | Single test — wrap in nested class |
| 19 | [StrictlyManagedResourceTests.cs](../test/Compze.Tests.Unit.Internals/SystemCE/StrictlyManagedResourceTests.cs) | 1 | Single test — `Given_a_StrictlyManagedResource` → `that_is_not_disposed` → `registers_uncatchable_exception_on_finalize` |
| 20 | [TaskCEExceptionsTests.cs](../test/Compze.Tests.Unit.Internals/SystemCE/ThreadingCE/TasksCE/TaskCEExceptionsTests.cs) | 1 | Single async test — wrap in nested class |

### Compze.Utilities.Tests

| # | File | Tests | Suggested BDD structure |
|---|------|-------|------------------------|
| 21 | [CartesianProductTests.cs](../test/Compze.Utilities.Tests/SystemCE/LinqCE/CartesianProductTests.cs) | 6 | **Also needs `[Fact]` → `[XF]` migration.** `CartesianProduct_specification` → `with_empty_input` / `with_single_list` / `with_two_lists` / `with_three_lists` / `with_list_containing_empty` |
| 22 | [When_calling_Must_NotBeEmpty.cs](../test/Compze.Utilities.Tests/Testing/Must/When_calling_Must_NotBeEmpty.cs) | 6 | `When_calling_Must_NotBeEmpty` → `on_non_empty_collection` (passes) / `on_empty_collection` (throws) — split by collection type |
| 23 | [When_calling_Must_BeEmpty.cs](../test/Compze.Utilities.Tests/Testing/Must/When_calling_Must_BeEmpty.cs) | 5 | Same pattern as above |
| 24 | [When_calling_DateTime_Must_Be_with_tolerance.cs](../test/Compze.Utilities.Tests/Testing/Must/When_calling_DateTime_Must_Be_with_tolerance.cs) | 7 | `When_calling_DateTime_Must_Be_with_tolerance` → `within_tolerance` (passes) / `at_boundary` / `outside_tolerance` (throws) |
| 25 | [When_calling_Must_HaveCount.cs](../test/Compze.Utilities.Tests/Testing/Must/When_calling_Must_HaveCount.cs) | 5 | `When_calling_Must_HaveCount` → `with_matching_count` (passes) / `with_different_count` (throws, check message) |

### Compze.Tests.CodePolicies

| # | File | Tests | Suggested BDD structure |
|---|------|-------|------------------------|
| 26 | [AppDomainExtensionsTests.cs](../test/Compze.Tests.CodePolicies/AppDomainExtensionsTests.cs) | 4 | Already has one level of nesting (`AllCompzeTypes`). Could add sub-groups: `returns_types_from_both_access_levels` / `returns_all_type_kinds` |

---

## Tier 2 — Medium Migrations (more setup complexity, larger files)

| # | File | Tests | Notes |
|---|------|-------|-------|
| 27 | [NestedEntitiesTests.cs](../test/Compze.Tests.Unit/CQRS/Taggregates/CompositeTaggregates/IntegerId/NestedEntitiesTests.cs) | 5 | Tests composite taggregates with integer IDs. Some setup complexity with entity creation sequences. |
| 28 | [MachineWideSharedObjectTests.cs](../test/Compze.Tests.Unit.Internals/SystemCE/ThreadingCE/MachineWideSharedObjectTests.cs) | 5 | Complex setup (file cleanup in constructor), shared state across processes. Tests could nest into `creating` / `sharing_data` / `persistence` / `blocking`. |
| 29 | [MonitorCE_specification.cs](../test/Compze.Tests.Unit.Internals/SystemCE/ThreadingCE/ResourceAccess/MonitorCE_specification.cs) | 5 | Partially nested already — the flat tests at the top level should be moved into nested context classes. |

### Compze.Utilities.Tests — Partially Nested

| # | File | Tests | Notes |
|---|------|-------|-------|
| 30 | [When_calling_Must_Equal_on_sequences.cs](../test/Compze.Utilities.Tests/Testing/Must/When_calling_Must_Equal_on_sequences.cs) | 5 | Has one nested class `when_sequences_differ` but 4 tests are flat at the root. Move those into nested classes too. |

### Performance Tests (flat `[XF]`)

| # | File | Tests | Notes |
|---|------|-------|-------|
| 31 | [Activator_default_constructor_Generic_argument_performance_tests.cs](../test/Compze.Tests.Performance.Internals/SystemCE/ReflectionCE/Activator_default_constructor_Generic_argument_performance_tests.cs) | 4 | Could nest: `can_construct` / `vs_default_constructor` / `vs_new_constraint` / `vs_activator_createinstance` |
| 32 | [Activator_one_argument_constructor_performance_tests.cs](../test/Compze.Tests.Performance.Internals/SystemCE/ReflectionCE/Activator_one_argument_constructor_performance_tests.cs) | 3 | Similar structure |
| 33 | [MachineWideSharedObjectPerformanceTests.cs](../test/Compze.Tests.Performance.Internals/SystemCE/ThreadingCE/MachineWideSharedObjectPerformanceTests.cs) | 4 | `get_copy` → `single_threaded` / `multi_threaded`, `update` → `single_threaded` / `multi_threaded` |
| 34 | [MonitorCEPerformanceTests.cs](../test/Compze.Tests.Performance.Internals/SystemCE/ThreadingCE/ResourceAccess/MonitorCEPerformanceTests.cs) | 8 | Could group: `read_operations` vs `increment_operations`, each with different locking strategies |
| 35 | [StrictlyManagedResourcePerformanceTests.cs](../test/Compze.Tests.Performance.Internals/StrictlyManagedResource/StrictlyManagedResourcePerformanceTests.cs) | 2 | `with_stack_traces` / `without_stack_traces` |
| 36 | [TimeAsserterTests.cs](../test/Compze.Tests.Performance.Internals/Testing/Performance/TimeAsserterTests.cs) | 2 | `Execute` / `ExecuteThreaded` |
| 37 | [ObjectNotDefaultPerformanceTests.cs](../test/Compze.Tests.Performance.Internals/Contracts/ObjectNotDefaultPerformanceTests.cs) | 1 | Single test |
| 38 | [NotNullOrDefaultPerformanceTests.cs](../test/Compze.Tests.Performance.Internals/Contracts/NotNullOrDefaultPerformanceTests.cs) | 1 | Single test |

---

## Tier 3 — Hard Migrations (`[PCT]` tests, complex fixtures, base class dependencies)

These use `[PCT]`/`[PCTSerializer]`/`[PCTDIContainer]` and inherit from `UniversalTestBase` with service locator patterns. BDD nesting is possible but the PCT attribute must be preserved, and setup often involves `TestEnv.DIContainer`.

### Compze.Tests.Integration

| # | File | Tests | Notes |
|---|------|-------|-------|
| 39 | [DocumentDbTests.cs](../test/Compze.Tests.Integration/Sql/DocumentDb/DocumentDbTests.cs) | ~35 | Largest integration test file. Would benefit greatly from BDD grouping (CRUD operations, transactions, concurrency). Inherits from `DocumentDbTestsBase`. |
| 40 | [TeventStoreUpdaterTest.cs](../test/Compze.Tests.Integration/CQRS/TeventStoreUpdaterTest.cs) | ~22 | Event store update operations. Complex fixture setup. |
| 41 | [TeventStoreTests.cs](../test/Compze.Tests.Integration/CQRS/TeventStoreTests.cs) | 8 | Core event store tests. |
| 42 | [TeventMigrationTest.cs](../test/Compze.Tests.Integration/CQRS/TeventRefactoring/Migrations/TeventMigrationTest.cs) | ~30 | Event migration scenarios with complex before/after inheritance trees. |
| 43 | [After_Creating_Two_Dbs_Named_DB1_And_DB2.cs](../test/Compze.Tests.Integration/Testing/Sql/After_Creating_Two_Dbs_Named_DB1_And_DB2.cs) | 6 | Already BDD-named, could nest into topic groups. |
| 44 | [DuplicateRegistrationTests.cs](../test/Compze.Tests.Integration/DependencyInjection/DuplicateRegistrationTests.cs) | 3 | DI duplicate registration checks. |
| 45 | [Navigator_specification.cs](../test/Compze.Tests.Integration/Tessaging/Navigator_specification.cs) | 3 | Service bus navigation. |
| 46 | Given_a_backend_endpoint tests (9 files in [ServiceBusSpecification/](../test/Compze.Tests.Integration/Tessaging/Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler/)) | ~30 | Already has BDD-style folder/file organization. Internal structure could be improved. |

### Compze.Tests.Performance.Internals (`[PCT]`)

| # | File | Tests | Notes |
|---|------|-------|-------|
| 47 | [Remote_Tuery_Performance_tests.cs](../test/Compze.Tests.Performance.Internals/Tessaging/Hypermedia/Remote_Tuery_Performance_tests.cs) | 11 | Could group: `single_threaded` / `multi_threaded` / `async` |
| 48 | [Local_Tuery_Performance_tests.cs](../test/Compze.Tests.Performance.Internals/Tessaging/Hypermedia/Local_Tuery_Performance_tests.cs) | 4 | `single_threaded` / `multi_threaded` |
| 49 | [TeventStoreTeventSerializerPerformanceTests.cs](../test/Compze.Tests.Performance.Internals/Serialization/TeventStoreTeventSerializerPerformanceTests.cs) | 2 | Serialization performance comparisons. |
| 50 | [TeventMigrationPerformanceTest.cs](../test/Compze.Tests.Performance.Internals/CQRS/TeventRefactoring/Migrations/TeventMigrationPerformanceTest.cs) | 3 | Complex event migration performance. |

### Compze.Tests.Unit.Internals (`[PCTSerializer]`)

| # | File | Tests | Notes |
|---|------|-------|-------|
| 51 | [NewtonSoftTeventStoreTeventSerializerTests.cs](../test/Compze.Tests.Unit.Internals/Serialization/NewtonSoftTeventStoreTeventSerializerTests.cs) | 1 | Single `[PCTSerializer]` test with service locator. Already nested-ish. |

---

## Not Candidates

| File | Reason |
|------|--------|
| [Compze.Tests.Common/](../test/Compze.Tests.Common/) (all files) | Base classes and shared fixtures — no executable tests |
| [Compze.Tests.Infrastructure/](../test/Compze.Tests.Infrastructure/) (all files) | Test infrastructure (`UniversalTestBase`, `[PCT]` attributes) — not test classes |
| [Compze.Tests.ScratchPad/](../test/Compze.Tests.ScratchPad/) (all files) | Experimental/design exploration — no real tests to migrate |
| [Compze.Utilities.Testing.XUnit.Tests/Placeholder.cs](../test/Compze.Utilities.Testing.XUnit.Tests/Placeholder.cs) | Just a "project compiles" placeholder |
| [Compze.Utilities.Tests/Testing/Xunit/ComponentCombinations/](../test/Compze.Utilities.Tests/Testing/Xunit/ComponentCombinations/) (~15 files) | Tests for the PCT framework itself — special-purpose |
| Files already in BDD-style (e.g., composite taggregate specs, ThreadGate, AsyncLockCE, Must assertion tests) | Already migrated |

---

## Migration Checklist

When migrating a file:

- [ ] Restructure into nested classes where each level inherits from parent
- [ ] Move setup into constructors at appropriate nesting levels
- [ ] Replace `[Fact]` with `[XF]` if any `[Fact]` attributes exist
- [ ] Ensure class names describe context (lowercase with underscores)
- [ ] Ensure method names describe expected behavior (lowercase with underscores)
- [ ] Test methods should be single-expression assertions using `Must()`
- [ ] Remove any `// Arrange`, `// Act`, `// Assert` comments
- [ ] Run tests to verify nothing broke
- [ ] Files with 1-2 tests: consider whether they're already well-structured as small focused files (namespace provides grouping)

## Recommended Migration Order

Start with **Tier 1 items 1–12** — these are small, self-contained, and provide good practice with the pattern before tackling more complex files. Items #14–16 (Contracts tests) are already well-structured as small focused files — the `Contracts/` namespace provides the grouping, and there's no shared context to build through nesting.

After Tier 1, move to **Tier 2** for the medium-complexity files, then Tier 3 for the PCT integration tests (which require more care around the pluggable component testing pattern).
