# Changelog

All notable changes to Compze.xUnitMatrix will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.11.0-beta

### Changed
- A matrix test whose configuration cannot be used now fails, with a message saying what is wrong. It used to be skipped,
  which a passing test run hides. That covers a configuration file that does not exist or lists no combinations, a line in
  it that is not a combination of the matrix's dimension values, a dimension type that is not an enum, and a `Skip<T>` for a
  type that is not one of the dimensions or with no reason. A configuration that was broken before now fails the test run.
- Each line of a configuration file must give exactly one value, or `*`, for each dimension, and each value must be the
  exact name of one of its dimension's values. The failure names the file, the line and what is wrong with it.

### Fixed
- A line with a value too many no longer runs as if the extra value were not there, and a line with a value too few no
  longer runs combinations short of a value. Both now fail the test. A line with a value too many and a `*` used to be
  skipped, with the message of an index out of range.
- A number in place of a value's name, such as `7`, no longer gives a value the enum does not define, and a
  comma-separated list of names, such as `MsSql,Postgre`, no longer gives their values combined, which could be another
  of the enum's values. Both now fail the test.
- Two matrices with different dimensions that name the same configuration file now each get the combinations of their own
  dimensions. The second one used to get the combinations read for the first.
- `*` for two dimensions of the same enum type now expands both. It used to be skipped, with the message "Sequence contains
  more than one matching element".

## 0.10.0-beta

### Changed
- Requires xUnit.net v3 4.0 or later (below 5.0). xUnit.net 4.0 changed the extensibility API in ways that are not
  binary compatible with 3.x, in either direction: 0.9.1-beta fails at runtime under xUnit.net 4.x, and this version
  fails under xUnit.net 3.x. Projects still on xUnit.net 3.x should stay on 0.9.1-beta.
- No longer depends on `Microsoft.NET.Test.Sdk`: a library should not push the test SDK onto its consumers. Test
  projects reference it themselves, as the xUnit.net project templates do.

### Fixed
- Matrix tests now expand into one test case per combination whatever the runner's `preEnumerateTheories` setting.
  They used to work only under runners that pre-enumerate theories, such as `dotnet test`; with it off, including a
  test executable run directly, every matrix test failed with an `IndexOutOfRangeException`.
- When xUnit.net still cannot expand a matrix test into its combinations (the attribute sets
  `DisableDiscoveryEnumeration`, or enumerating the combinations threw), the test now fails with a message explaining
  why, instead of crashing when it runs.

## 0.9.1-beta

- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.9.0-beta

### Added
- XML documentation across the entire public API.

### Changed
- Minimized the public API and harmonized naming around the dimension / dimension value / combination / matrix
  vocabulary. Discovery, execution, configuration-file parsing, and skip matching are now internal.

### Removed
- The `useTestMethodArgument` option and support for passing the combination as a test-method argument. Test methods
  are parameterless; read the current combination via `MatrixCombination.Current` or the `CurrentDimensionValueN`
  properties on the generic `MatrixTheoryAttribute<…>` bases.
- `SkipValues(...)` programmatic skipping. Skip combinations per test method with the `[Skip<T>]` attribute instead.

## 0.5.0-beta

- Initial release
