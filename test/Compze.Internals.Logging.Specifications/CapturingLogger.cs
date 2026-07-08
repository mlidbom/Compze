using System.Diagnostics;

namespace Compze.Internals.Logging.Specifications;

/// <summary>
/// A test backend that captures every log call so specifications can assert on the structured
/// template + values shape produced by the interpolated string handlers, and the ambient
/// <see cref="Activity.Current"/> correlation. This is the cleanest way to verify handler and
/// correlation output without involving Serilog or Console.
/// </summary>
class CapturingLogger : Logger
{
   public List<CapturedLogCall> Captured { get; } = [];

   public CapturingLogger() : base(LogLevel.Debug) {}
   public CapturingLogger(LogLevel level) : base(level) {}

   public override ILogger WithLogLevel(LogLevel level) => new CapturingLogger(level) { _shared = _shared ?? this };
   CapturingLogger? _shared;

   protected override void CriticalInternal(Exception? exception, string template, object?[]? values, string caller)
      => Add(LogLevel.Critical, template, values, caller, exception);

   protected override void ErrorInternal(Exception exception, string template, object?[]? values, string caller)
      => Add(LogLevel.Error, template, values, caller, exception);

   protected override void WarningInternal(Exception? exception, string template, object?[]? values, string caller)
      => Add(LogLevel.Warning, template, values, caller, exception);

   protected override void InfoInternal(string template, object?[]? values, string caller)
      => Add(LogLevel.Info, template, values, caller, exception: null);

   protected override void DebugInternal(string template, object?[]? values, string caller)
      => Add(LogLevel.Debug, template, values, caller, exception: null);

   protected override void TraceInternal(string template, object?[]? values, string caller)
      => Add(LogLevel.Trace, template, values, caller, exception: null);

   void Add(LogLevel level, string template, object?[]? values, string caller, Exception? exception)
      => (_shared ?? this).Captured.Add(new(level, template, values, caller, exception, Activity.Current?.OperationName, Activity.Current?.Id));
}

record CapturedLogCall(LogLevel Level, string? Template, object?[]? Values, string Caller, Exception? Exception, string? ActivityName, string? ActivityId);
