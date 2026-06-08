namespace Compze.Internals.Logging.Specifications;

/// <summary>
/// A test backend that captures every log call so specifications can assert on the structured
/// template + values shape produced by the interpolated string handlers, plus any ambient properties
/// attached via <see cref="ILogger.WithProperty"/>. This is the cleanest way to verify handler and
/// correlation output without involving Serilog or Console.
/// </summary>
class CapturingLogger : Logger
{
   public List<CapturedLogCall> Captured { get; } = [];
   readonly (string Name, object? Value)[] _properties;

   public CapturingLogger() : base(LogLevel.Debug) => _properties = [];
   CapturingLogger(LogLevel? level, (string Name, object? Value)[] properties) : base(level) => _properties = properties;

   public override ILogger WithLogLevel(LogLevel level) => new CapturingLogger(level, _properties) { _shared = _shared ?? this };
   public override ILogger WithProperty(string name, object? value) => new CapturingLogger(ConfiguredLogLevel, [.._properties, (name, value)]) { _shared = _shared ?? this };
   CapturingLogger? _shared;

   protected override void ErrorInternal(Exception exception, string template, object?[]? values, string caller)
      => Add(new(LogLevel.Error, template, values, caller, exception, _properties));

   protected override void WarningInternal(Exception? exception, string template, object?[]? values, string caller)
      => Add(new(LogLevel.Warning, template, values, caller, exception, _properties));

   protected override void InfoInternal(string template, object?[]? values, string caller)
      => Add(new(LogLevel.Info, template, values, caller, Exception: null, _properties));

   protected override void DebugInternal(string template, object?[]? values, string caller)
      => Add(new(LogLevel.Debug, template, values, caller, Exception: null, _properties));

   void Add(CapturedLogCall call) => (_shared ?? this).Captured.Add(call);
}

record CapturedLogCall(LogLevel Level, string? Template, object?[]? Values, string Caller, Exception? Exception, IReadOnlyList<(string Name, object? Value)> Properties);
