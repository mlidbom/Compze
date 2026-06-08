using System.Diagnostics;

namespace Compze.Internals.Logging;

///<summary>
/// One open timed span created by the <see cref="LevelLoggerExtensions"/> <c>Time</c>/<c>TimeAsync</c> family:
/// logs a "started" line when begun and a "took N.Nms" (or "FAULTED after ...") line when ended. Both lines are
/// prefixed with the span's ancestry path — <c>#root/#parent/#self</c>, each segment a process-unique id — so a
/// span's parent is unambiguous even when sibling spans at the same nesting level interleave across threads and
/// awaits, and the two lines of one span are paired by their shared path. The innermost open span is tracked per
/// logical call flow with an <see cref="AsyncLocal{T}"/>, so a span's parent is whichever span was open where it
/// began.
///</summary>
sealed class TimedScope
{
   static int _lastSpanId;
   static readonly AsyncLocal<TimedScope?> CurrentScope = new();

   readonly ILevelLogger _level;
   readonly string _label;
   readonly string _caller;
   readonly string _spanPath;
   readonly TimedScope? _parent;
   readonly long _startedTimestamp;

   TimedScope(ILevelLogger level, string label, string caller, string spanPath, TimedScope? parent, long startedTimestamp)
   {
      _level = level;
      _label = label;
      _caller = caller;
      _spanPath = spanPath;
      _parent = parent;
      _startedTimestamp = startedTimestamp;
   }

   public static TimedScope Begin(ILevelLogger level, string label, string caller)
   {
      var spanId = Interlocked.Increment(ref _lastSpanId);
      var parent = CurrentScope.Value;
      var spanPath = parent is null ? $"#{spanId}" : $"{parent._spanPath}/#{spanId}";
      LogStarted(level, spanPath, label, caller);
      var scope = new TimedScope(level, label, caller, spanPath, parent, Stopwatch.GetTimestamp());
      CurrentScope.Value = scope;
      return scope;
   }

   public void End(Exception? fault)
   {
      CurrentScope.Value = _parent;
      var elapsedMs = Stopwatch.GetElapsedTime(_startedTimestamp).TotalMilliseconds;
      if(fault is null)
      {
         LogTook(_level, _spanPath, _label, elapsedMs, _caller);
      }
      else
      {
         LogFaulted(_level, _spanPath, _label, elapsedMs, fault.GetType().Name, _caller);
      }
   }

   // Static helpers so the interpolation-hole expressions are clean identifiers: that is what names the structured
   // log properties (spanPath/label/elapsedMs/faultType), which fields (_spanPath, ...) would not.
   static void LogStarted(ILevelLogger level, string spanPath, string label, string caller)
      => level.Log($"{spanPath} {label} started", caller);

   static void LogTook(ILevelLogger level, string spanPath, string label, double elapsedMs, string caller)
      => level.Log($"{spanPath} {label} took {elapsedMs:F1}ms", caller);

   static void LogFaulted(ILevelLogger level, string spanPath, string label, double elapsedMs, string faultType, string caller)
      => level.Log($"{spanPath} {label} FAULTED after {elapsedMs:F1}ms: {faultType}", caller);
}
