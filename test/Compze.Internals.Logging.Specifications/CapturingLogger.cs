namespace Compze.Internals.Logging.Specifications;

/// <summary>
/// A test backend that captures every log call so specifications can assert on the structured
/// template + values shape produced by the interpolated string handlers. This is the cleanest
/// way to verify handler output without involving Serilog or Console.
/// </summary>
class CapturingLogger : Logger
{
   public List<CapturedLogCall> Captured { get; } = [];

   public CapturingLogger() : base(LogLevel.Debug) {}
   CapturingLogger(LogLevel level) : base(level) {}

   public override ILogger WithLogLevel(LogLevel level) => new CapturingLogger(level) { _shared = this };
   CapturingLogger? _shared;

   protected override void ErrorInternal(Exception exception, string? template, object?[]? values, string caller)
      => Add(new(LogLevel.Error, template, values, caller, exception));

   protected override void WarningInternal(Exception? exception, string template, object?[]? values, string caller)
      => Add(new(LogLevel.Warning, template, values, caller, exception));

   protected override void InfoInternal(string template, object?[]? values, string caller)
      => Add(new(LogLevel.Info, template, values, caller, Exception: null));

   protected override void DebugInternal(string template, object?[]? values, string caller)
      => Add(new(LogLevel.Debug, template, values, caller, Exception: null));

   void Add(CapturedLogCall call) => (_shared ?? this).Captured.Add(call);
}

record CapturedLogCall(LogLevel Level, string? Template, object?[]? Values, string Caller, Exception? Exception);
