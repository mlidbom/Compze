# Changelog

All notable changes to Compze.Internals.Logging.Serilog will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.4.1-alpha

- Released in lockstep with [Compze.Internals.Logging](https://www.nuget.org/packages/Compze.Internals.Logging/) 0.4.1-alpha. `SerilogLogger` extends that package's abstract `Logger`, so the two must be upgraded together — a backend compiled against a different core fails to load.

## 0.4.0-alpha

### Changed
- Version bumped in lockstep with `Compze.Internals.Logging` 0.4.0-alpha. `SerilogLogger` implements the two backend hooks the base `Logger` now requires — `CriticalInternal` (mapped to Serilog's `Fatal`) and `TraceInternal` (mapped to Serilog's `Verbose`) — so consuming applications MUST update this package and `Compze.Internals.Logging` together; a new core against this package at 0.3.2-alpha throws `TypeLoadException` at load because the old `SerilogLogger` lacks those implementations.

## 0.3.2-alpha

- Version bumped in lockstep with `Compze.Internals.Logging` 0.3.2-alpha. No changes to the Serilog integration itself.

## 0.3.1-alpha

### Changed
- `SerilogLogger` now enriches every log event with the ambient `System.Diagnostics.Activity.Current` (when one is set) as `Activity` (the operation name) and `ActivityId` properties, alongside the existing `CallerMember`. Logs emitted anywhere within a `StartActivity(...)` scope are correlated automatically; add `{Activity}` / `{ActivityId}` to your output template to render them.

## 0.2.0-alpha

### Added
- `SerilogLogger.Create(Type, Serilog.ILogger)` overload for wiring an explicit Serilog `ILogger` instance instead of reading from the global `Log.Logger`.

### Changed
- Structured property capture is now preserved end-to-end. Interpolated calls and explicit `(template, values)` calls forward to Serilog with the original template text and structured property values intact; Serilog's template cache and structured sinks (Seq, Elasticsearch, Application Insights, etc.) now see the named properties as intended. Previously the wrapper rendered every message to a string before handing it to Serilog, which defeated both the template cache and the structured-property pipeline.
- Plain non-interpolated string messages still pass through unchanged, with `{` / `}` escaped so Serilog does not attempt to parse holes that aren't there.

## 0.1.0-alpha.3

- Initial pre-release
