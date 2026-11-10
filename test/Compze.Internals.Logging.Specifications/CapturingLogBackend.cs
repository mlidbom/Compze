using System.Diagnostics;

namespace Compze.Internals.Logging.Specifications;

///<summary>
/// A test <see cref="ILogBackend"/> that records every <see cref="LogLine"/> written to it, so specifications can assert on
/// what the loggers hand a backend: level, <see cref="LogLine.MessageTemplate"/> and <see cref="LogLine.TemplateValues"/>,
/// exception, and the ambient <see cref="Activity.Current"/> at the time.
///</summary>
///<remarks>
/// <see cref="Install"/> makes one the process's <see cref="CompzeLogger.Backend"/>, which is why the specifications in this
/// assembly run one at a time (see <c>SpecificationsShareTheProcessWideLogBackend.cs</c>).
///</remarks>
sealed class CapturingLogBackend : ILogBackend
{
   readonly List<CapturedLogLine> _captured = [];

   CapturingLogBackend() {}

   ///<summary>Makes a new <see cref="CapturingLogBackend"/> the <see cref="CompzeLogger.Backend"/>, and returns it.</summary>
   public static CapturingLogBackend Install()
   {
      var backend = new CapturingLogBackend();
      CompzeLogger.Backend = backend;
      return backend;
   }

   ///<summary>Every line written to this backend, in the order they were written.</summary>
   public IReadOnlyList<CapturedLogLine> Captured => _captured;

   public void Write(LogLine line) => _captured.Add(new CapturedLogLine(line, Activity.Current));
}

///<summary>A <see cref="LogLine"/> a <see cref="CapturingLogBackend"/> recorded, with the <see cref="Activity.Current"/> it was written within.</summary>
sealed class CapturedLogLine
{
   readonly LogLine _line;

   internal CapturedLogLine(LogLine line, Activity? currentActivity)
   {
      _line = line;
      ActivityName = currentActivity?.OperationName;
      ActivityId = currentActivity?.Id;
   }

   public LogLevel Level => _line.Level;
   public string MessageTemplate => _line.MessageTemplate;
   public IReadOnlyList<object?> TemplateValues => _line.TemplateValues;
   public string CallerMember => _line.CallerMember;
   public Exception? Exception => _line.Exception;

   ///<summary>The <see cref="Activity.OperationName"/> of the <see cref="Activity.Current"/> the line was written within, or null when there was none.</summary>
   public string? ActivityName { get; }

   ///<summary>The <see cref="Activity.Id"/> of the <see cref="Activity.Current"/> the line was written within, or null when there was none.</summary>
   public string? ActivityId { get; }
}
