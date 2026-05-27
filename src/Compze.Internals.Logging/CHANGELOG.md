# Changelog

All notable changes to Compze.Internals.Logging will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.2.0-alpha.1

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
