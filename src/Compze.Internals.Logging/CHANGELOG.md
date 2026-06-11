# Changelog

All notable changes to Compze.Internals.Logging will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.3.1-alpha

### Added
- `ExecutionTime` / `ExecutionTimeAsync` and `MethodExecutionTime` / `MethodExecutionTimeAsync` timing spans on `ILevelLogger`. `ExecutionTime` times a region — a delegate (its source text becomes the label) or a `using` scope. `MethodExecutionTime` times the enclosing method as a whole, labelled with the method's name, so `void Render() => Log.MethodExecutionTime(() => { ... });` instruments a method without changing a line of its body. Each span logs a "started" line and a "took N.Nms" line, prefixed with an ancestry path (`#root/#parent/#self`) so nested and interleaved spans stay unambiguous; the duration is captured as a structured `elapsedMs` property. Faults are logged with the exception type and re-thrown. Disabled levels allocate nothing.
- `StartActivity` / `IActivityScope`: trace a named, ongoing process as a whole, backed by `System.Diagnostics.Activity`. Logs an "activity started" line and makes the activity the ambient `Activity.Current`, so every line logged by any logger during the scope — across awaits and threads — is tagged with the activity name (`Activity`) and a unique id (`ActivityId`) with no handle threading. Supports repeatable `LogElapsed(milestone)`, `Fail(exception)` (sets the activity's status to error), and a completion-or-failure line with total elapsed time on dispose.
- `IActivityScope.MakeCurrent()`: re-establishes a held activity as the ambient `Activity.Current` for a `using` scope, so lines logged on a path that did not inherit it — a later dispatcher turn, a timer tick, a fresh callback — are tagged with the activity. Closes the auto-tagging gap for flows that span multiple dispatcher turns.

### Removed
- The provisional `LogMethodEntryExit` / `LogMethodExecutionTime` / `LogEntryExit` extension methods, superseded by the `ExecutionTime` / `MethodExecutionTime` family.

## 0.2.0-alpha

### Added
- Structured logging via C# interpolated string handler overloads on `ILogger` and `ILevelLogger`. Existing call sites that use `$"..."` syntax now silently gain structured property capture — values inside holes become named properties (captured from the C# expression text via `CallerArgumentExpression`), and the original template text is preserved end-to-end.
- `bool IsEnabled(LogLevel)` on `ILogger`.
- Explicit `(string template, object?[] values, ...)` overloads on each level for callers that need to pass a non-interpolated template.

### Changed
- When a level is disabled (or `LoggingSuppressed` is active), expressions inside interpolation holes are no longer evaluated. Previously every interpolated string was fully formatted at the call site before the level check ran; now the level check happens in the handler constructor and skips both formatting and any work inside the holes.
- The handler-based call path is allocation-free on disabled calls, and on enabled calls reuses a thread-local `StringBuilder` from a small pool.
- `ConsoleLogger` is now `public` (previously internal) so it can be constructed directly without going through the global `CompzeLogger.LoggerFactoryMethod`.
- `ConsoleLogger` now renders Serilog-style templates with format specifiers (`{x:F1}`), alignment (`{x,8}`), and brace-escaping (`{{`, `}}`). Numeric formatting uses `CultureInfo.InvariantCulture` so log output is locale-independent.
- The internal `Logger.*Internal` abstract method signatures changed from `(string message, string caller)` to `(string template, object?[]? values, string caller)`. Affects only custom backends that derive from `Logger`.

## 0.1.0-alpha.3

- Initial pre-release
