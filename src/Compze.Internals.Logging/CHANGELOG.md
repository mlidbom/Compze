# Changelog

All notable changes to Compze.Internals.Logging will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.5.0-alpha

### Fixed
- A logger made before the host chose its logging backend wrote to the console for good, without a word. `CompzeLogger.For<T>()` and `this.Log()` made each type's logger once, from whatever `CompzeLogger.LoggerFactoryMethod` returned at that moment, so a type touched before logging was configured — through a `static readonly` logger field, or by logging — logged to the console for the rest of the process. Loggers now hold no backend: each line goes to the `CompzeLogger.Backend` current when it is written, so a logger can be made at any time, a `static readonly` logger field included.
- The console backend printed the doubled braces of an interpolated message with no holes as they were (`$"literal {{x}}"` printed `literal {{x}}`). It now prints single braces.

### Changed
- The logging backend is its own concept, separate from the `ILogger` code logs through: `ILogBackend`, with one method, `Write(LogLine)`. A `LogLine` carries the line's level, the type its logger was made for, the member that logged it, its message template and values, and its exception. Writing a backend means implementing `ILogBackend`; the level and suppression checks are made before a line reaches it.
- The process's backend is `CompzeLogger.Backend`, which replaces `CompzeLogger.LoggerFactoryMethod`: `CompzeLogger.Backend = SerilogLogBackend.Global;` instead of `CompzeLogger.LoggerFactoryMethod = SerilogLogger.Create;`. It defaults to `ILogBackend.Console`.
- A message logged as plain text reaches the backend as a message template with its braces doubled and no values, the same shape as every other line.

### Removed
- The abstract `Logger` base class, which backends used to extend: implement `ILogBackend` instead.
- `ConsoleLogger`: the console backend is `ILogBackend.Console`.

## 0.4.1-alpha

- `LevelLogger`'s implementation moves below `_private`; the logging vocabulary keeps its public shape.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.4.0-alpha

### Added
- A full logging surface for the two extreme levels, which previously existed only as threshold values with no way to emit at them: `ILogger.Trace` / `Critical` overloads, `TraceLogInterpolatedStringHandler` / `CriticalLogInterpolatedStringHandler`, `ILevelLogger` views (`logger.Trace()` / `logger.Critical()`), and rendering in both the `ConsoleLogger` (`TRC` / `CRT`) and `SerilogLogger` backends. `Trace` (below `Debug`) is for very-high-volume events that would otherwise drown `Debug`; `Critical` (above `Error`) is the "wake a human" tier. `Critical` mirrors `Warning` (with and without an exception); `Trace` mirrors `Debug`.

### Changed
- Renamed the two extreme `LogLevel` members to the `Microsoft.Extensions.Logging` vocabulary: `Verbose` → `Trace`, `Fatal` → `Critical`.
- `LogLevel`'s numeric values now ascend with severity (`Trace` = 0 … `Critical` = 5), matching `Microsoft.Extensions.Logging.LogLevel` and syslog — previously they descended. A configured `LogLevel` is a *minimum severity* floor, so `IsEnabled` is now `level >= configuredLevel`. Behavior at every named level is unchanged; only code that compared raw `LogLevel` magnitudes would be affected, and there is none.
- The `SerilogLogger` backend maps Compze's `Trace` to Serilog's `Verbose` and Compze's `Critical` to Serilog's `Fatal` (the frameworks name these two levels differently; the translation is confined to that backend).

## 0.3.2-alpha

### Changed
- Renamed the region-timing spans `Time` / `TimeAsync` to `ExecutionTime` / `ExecutionTimeAsync`, so the call reads as a verb supplied by the logger: `Log.ExecutionTime(() => Decode(path))`.

### Added
- `MethodExecutionTime` / `MethodExecutionTimeAsync`: time the enclosing method as a whole, labelled with the method's name (defaulted from `[CallerMemberName]`), so `void Render() => Log.MethodExecutionTime(() => { ... });` instruments a method without changing a line of its body. Delegate, async, and `using`-scope forms.

## 0.3.1-alpha

### Added
- `Time` / `TimeAsync` timing spans on `ILevelLogger`: wrap a delegate (the operation's source text becomes the label) or open a `using` scope to log how long a region of code takes. Each span logs a "started" line and a "took N.Nms" line, prefixed with an ancestry path (`#root/#parent/#self`) so nested and interleaved spans stay unambiguous; the duration is captured as a structured `elapsedMs` property. Faults are logged with the exception type and re-thrown. Disabled levels allocate nothing.
- `StartActivity` / `IActivityScope`: trace a named, ongoing process as a whole, backed by `System.Diagnostics.Activity`. Logs an "activity started" line and makes the activity the ambient `Activity.Current`, so every line logged by any logger during the scope — across awaits and threads — is tagged with the activity name (`Activity`) and a unique id (`ActivityId`) with no handle threading. Supports repeatable `LogElapsed(milestone)`, `Fail(exception)` (sets the activity's status to error), and a completion-or-failure line with total elapsed time on dispose.
- `IActivityScope.MakeCurrent()`: re-establishes a held activity as the ambient `Activity.Current` for a `using` scope, so lines logged on a path that did not inherit it — a later dispatcher turn, a timer tick, a fresh callback — are tagged with the activity. Closes the auto-tagging gap for flows that span multiple dispatcher turns.

### Removed
- The provisional `LogMethodEntryExit` / `LogMethodExecutionTime` / `LogEntryExit` extension methods, superseded by the `Time` family.

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
