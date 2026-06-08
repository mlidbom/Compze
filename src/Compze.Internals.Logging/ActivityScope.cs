using System.Diagnostics;
using System.Runtime.CompilerServices;
using Compze.SystemCE;

// ReSharper disable UnusedMember.Global Diagnostics API — used occasionally while researching performance, and by sibling apps built on our logging.
// ReSharper disable UnusedType.Global

namespace Compze.Internals.Logging;

///<summary>
/// Traces a named, ongoing process — an activity — as a whole. Started via <see cref="ActivityScopeExtensions.StartActivity"/>,
/// which begins a <see cref="System.Diagnostics.Activity"/> and logs an "activity started" line. While it runs the
/// activity is the ambient <see cref="Activity.Current"/>, so every log line emitted by any Compze logger — across
/// awaits and threads — is automatically tagged with the activity's name and a unique id, without threading this handle
/// anywhere. Read elapsed time with <see cref="LogElapsed"/>, mark failure with <see cref="Fail"/>, and dispose it (the
/// end of a <c>using</c> block) to stop the activity and log how long it took — or that it failed. Add <c>{Activity}</c>
/// to your output template to see the name on each line.
///</summary>
public interface IActivityScope : IDisposable
{
   ///<summary>The time elapsed since the activity was started.</summary>
   TimeSpan Elapsed { get; }

   ///<summary>Logs "<paramref name="milestone"/> (+N.Nms)" — the milestone reached and how far into the activity it was reached. Repeatable.</summary>
   Unit LogElapsed(string milestone, [CallerMemberName] string caller = "");

   ///<summary>Marks the activity as failed because of <paramref name="exception"/>: disposing then logs that it failed rather than completed, and sets the underlying activity's status to error.</summary>
   void Fail(Exception exception);

   internal static IActivityScope Start(ILogger parentLogger, string activityName, LogLevel level) => ActivityScope.Start(parentLogger, activityName, level);

   private sealed class ActivityScope : IActivityScope
   {
      readonly ILevelLogger _log;
      readonly Activity _activity;
      readonly long _startedTimestamp;
      Exception? _failure;
      int _stopped;

      ActivityScope(ILevelLogger log, Activity activity, long startedTimestamp)
      {
         _log = log;
         _activity = activity;
         _startedTimestamp = startedTimestamp;
      }

      internal static IActivityScope Start(ILogger logger, string activityName, LogLevel level)
      {
#pragma warning disable CA2000 // The Activity is owned by the returned IActivityScope, which disposes it (restoring the previous Activity.Current) when the scope is disposed.
         var activity = new Activity(activityName).Start();
#pragma warning restore CA2000
         var scope = new ActivityScope(LevelView(logger, level), activity, Stopwatch.GetTimestamp());
         scope._log.Log("activity started");
         return scope;
      }

      static ILevelLogger LevelView(ILogger logger, LogLevel level) => level switch
      {
         LogLevel.Warning => logger.Warning(),
         LogLevel.Info    => logger.Info(),
         _                => logger.Debug()
      };

      public TimeSpan Elapsed => Stopwatch.GetElapsedTime(_startedTimestamp);

      public Unit LogElapsed(string milestone, [CallerMemberName] string caller = "")
      {
         if(!_log.IsEnabled()) return unit;
         var elapsedMs = Stopwatch.GetElapsedTime(_startedTimestamp).TotalMilliseconds;
         return _log.Log($"{milestone} (+{elapsedMs:F1}ms)", caller);
      }

      public void Fail(Exception exception) => _failure = exception;

      public void Dispose()
      {
         if(Interlocked.CompareExchange(ref _stopped, 1, 0) != 0) return;
         var elapsedMs = Stopwatch.GetElapsedTime(_startedTimestamp).TotalMilliseconds;
         if(_failure is {} failure)
         {
            _activity.SetStatus(ActivityStatusCode.Error, failure.Message);
            var failureType = failure.GetType().Name;
            if(_log.IsEnabled()) _log.Log($"activity failed after {elapsedMs:F1}ms: {failureType}");
         }
         else
         {
            _activity.SetStatus(ActivityStatusCode.Ok);
            if(_log.IsEnabled()) _log.Log($"activity completed in {elapsedMs:F1}ms");
         }
         _activity.Dispose();
      }
   }
}

public static class ActivityScopeExtensions
{
   ///<summary>Starts an <see cref="IActivityScope"/> for a named process: begins a <see cref="System.Diagnostics.Activity"/>, logs "activity started", and makes the activity ambient so every subsequent log line — from any logger, across awaits and threads — is tagged with its name and id until the scope is disposed.</summary>
   public static IActivityScope StartActivity(this ILogger @this, string activityName, LogLevel level = LogLevel.Debug)
      => IActivityScope.Start(@this, activityName, level);
}
