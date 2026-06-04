# Changelog

All notable changes to Compze.Internals.Logging.Serilog will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.2.0-alpha

### Added
- `SerilogLogger.Create(Type, Serilog.ILogger)` overload for wiring an explicit Serilog `ILogger` instance instead of reading from the global `Log.Logger`.

### Changed
- Structured property capture is now preserved end-to-end. Interpolated calls and explicit `(template, values)` calls forward to Serilog with the original template text and structured property values intact; Serilog's template cache and structured sinks (Seq, Elasticsearch, Application Insights, etc.) now see the named properties as intended. Previously the wrapper rendered every message to a string before handing it to Serilog, which defeated both the template cache and the structured-property pipeline.
- Plain non-interpolated string messages still pass through unchanged, with `{` / `}` escaped so Serilog does not attempt to parse holes that aren't there.

## 0.1.0-alpha.3

- Initial pre-release
