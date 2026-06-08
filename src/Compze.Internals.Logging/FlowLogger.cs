using System.Diagnostics;
using System.Runtime.CompilerServices;
using Compze.SystemCE;

// ReSharper disable UnusedMember.Global Diagnostics API — used occasionally while researching performance, and by sibling apps built on our logging.
// ReSharper disable UnusedType.Global

namespace Compze.Internals.Logging;

///<summary>
/// A logger scoped to a named, ongoing process — a "flow" — that you trace as a whole. Entered via
/// <see cref="FlowLoggerExtensions.StartFlowLogger"/>, which logs a "flow started" line and tags <em>every</em> line
/// logged through it (including <c>Time</c> spans) with the flow's name as a structured <c>Flow</c> property, so
/// the whole process can be filtered out of an interleaved log. Read elapsed time since the flow began at any
/// point with <see cref="LogElapsed"/>, and dispose it (the end of a <c>using</c> block) to log how long the
/// whole flow took. Add <c>{Flow}</c> to your output template to see the name on each line.
///</summary>
public interface IFlowLogger : ILevelLogger, IDisposable
{
   ///<summary>The time elapsed since the flow was started.</summary>
   TimeSpan Elapsed { get; }

   ///<summary>Logs "<paramref name="milestone"/> (+N.Nms)" — the milestone reached and how long into the flow it was reached. Repeatable.</summary>
   Unit LogElapsed(string milestone, [CallerMemberName] string caller = "");

   internal static IFlowLogger Start(ILogger parentLogger, string flowName, LogLevel level = LogLevel.Debug) => FlowLogger.Start(parentLogger, flowName, level);

   private sealed class FlowLogger : IFlowLogger
   {
      readonly ILevelLogger _inner;
      readonly long _startedTimestamp;
      int _completed;

      FlowLogger(ILevelLogger inner, long startedTimestamp)
      {
         _inner = inner;
         _startedTimestamp = startedTimestamp;
      }

      internal static IFlowLogger Start(ILogger logger, string flowName, LogLevel level)
      {
         var inner = LevelView(logger.WithProperty("Flow", flowName), level);
         var flow = new FlowLogger(inner, Stopwatch.GetTimestamp());
         inner.Log("flow started");
         return flow;
      }

      static ILevelLogger LevelView(ILogger logger, LogLevel level) => level switch
      {
         LogLevel.Warning => logger.Warning(),
         LogLevel.Info    => logger.Info(),
         _                => logger.Debug()
      };

      public bool IsEnabled() => _inner.IsEnabled();
      public Unit Log(string message, [CallerMemberName] string caller = "") => _inner.Log(message, caller);
      public Unit Log([InterpolatedStringHandlerArgument("")] ref LevelLogInterpolatedStringHandler handler, [CallerMemberName] string caller = "") => _inner.Log(ref handler, caller);

      public TimeSpan Elapsed => Stopwatch.GetElapsedTime(_startedTimestamp);

      public Unit LogElapsed(string milestone, [CallerMemberName] string caller = "")
      {
         if(!_inner.IsEnabled()) return unit;
         var elapsedMs = Stopwatch.GetElapsedTime(_startedTimestamp).TotalMilliseconds;
         return _inner.Log($"{milestone} (+{elapsedMs:F1}ms)", caller);
      }

      public void Dispose()
      {
         if(Interlocked.CompareExchange(ref _completed, 1, 0) != 0 || !_inner.IsEnabled()) return;
         var elapsedMs = Stopwatch.GetElapsedTime(_startedTimestamp).TotalMilliseconds;
         _inner.Log($"flow completed in {elapsedMs:F1}ms");
      }
   }
}


public static class FlowLoggerExtensions
{
   ///<summary>Starts a <see cref="IFlowLogger"/> for a named process: logs "flow started" now and tags everything logged through the returned logger — <c>Time</c> spans included — with the structured <c>Flow</c> property <paramref name="flowName"/>. Dispose it to log how long the flow took.</summary>
   public static IFlowLogger StartFlowLogger(this ILogger @this, string flowName, LogLevel level = LogLevel.Debug)
      => IFlowLogger.Start(@this, flowName, level);
}
