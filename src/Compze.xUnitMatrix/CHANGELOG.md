# Changelog

All notable changes to Compze.xUnitMatrix will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

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