using System.Runtime.CompilerServices;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.SystemCE;

// ReSharper disable UnusedMember.Global Diagnostics API — used occasionally while researching performance, and by sibling apps built on our logging.
// ReSharper disable UnusedType.Global

namespace Compze.Internals.Logging;

///<summary>
/// Convenience instrumentation on top of <see cref="ILevelLogger"/>: wrap a delegate — or open a <c>using</c>
/// scope — to log how long a region of code takes. Each timed region logs two lines: "started" when entered and
/// "took N.Nms" when it ends, both prefixed with the region's ancestry path (<c>#root/#parent/#self</c>) so the
/// pair is matched and its parent identified even when many regions interleave across threads and awaits. The
/// duration is captured as a structured <c>elapsedMs</c> property, so it can be aggregated, not just read. When
/// the level is disabled the delegate runs with zero added cost: no stopwatch, no allocation, no lines.
///</summary>
///<remarks>
/// Cache the level logger in a field and call <c>Time</c> on it: <c>readonly ILevelLogger _debug = For&lt;X&gt;().Debug();</c>
/// then <c>_debug.Time(() =&gt; Decode(path))</c>. Caching is safe — whether the level is enabled is still decided
/// live on each call, not frozen when the field is built — and it avoids re-allocating the level-logger wrapper.
///</remarks>
public static class LevelLoggerExtensions
{
   extension(ILevelLogger @this)
   {
      ///<summary>Runs <paramref name="operation"/> as a timed span labelled with its own source text, and returns its result.</summary>
      public T Time<T>(Func<T> operation,
                       [CallerArgumentExpression(nameof(operation))] string label = "",
                       [CallerMemberName] string caller = "")
      {
         if(!@this.IsEnabled()) return operation();
         var scope = TimedScope.Begin(@this, label, caller);
         try
         {
            var result = operation();
            scope.End(fault: null);
            return result;
         }
         catch(Exception exception)
         {
            scope.End(exception);
            throw;
         }
      }

      ///<summary>Runs <paramref name="operation"/> as a timed span labelled with its own source text.</summary>
      public Unit Time(Action operation,
                       [CallerArgumentExpression(nameof(operation))] string label = "",
                       [CallerMemberName] string caller = "")
      {
         if(!@this.IsEnabled())
         {
            operation();
            return unit;
         }
         var scope = TimedScope.Begin(@this, label, caller);
         try
         {
            operation();
            scope.End(fault: null);
            return unit;
         }
         catch(Exception exception)
         {
            scope.End(exception);
            throw;
         }
      }

      ///<summary>Awaits <paramref name="operation"/> as a timed span labelled with its own source text, and returns its result.</summary>
      public async Task<T> TimeAsync<T>(Func<Task<T>> operation,
                                        [CallerArgumentExpression(nameof(operation))] string label = "",
                                        [CallerMemberName] string caller = "")
      {
         if(!@this.IsEnabled()) return await operation().caf();
         var scope = TimedScope.Begin(@this, label, caller);
         try
         {
            var result = await operation().caf();
            scope.End(fault: null);
            return result;
         }
         catch(Exception exception)
         {
            scope.End(exception);
            throw;
         }
      }

      ///<summary>Awaits <paramref name="operation"/> as a timed span labelled with its own source text.</summary>
      public async Task TimeAsync(Func<Task> operation,
                                  [CallerArgumentExpression(nameof(operation))] string label = "",
                                  [CallerMemberName] string caller = "")
      {
         if(!@this.IsEnabled())
         {
            await operation().caf();
            return;
         }
         var scope = TimedScope.Begin(@this, label, caller);
         try
         {
            await operation().caf();
            scope.End(fault: null);
         }
         catch(Exception exception)
         {
            scope.End(exception);
            throw;
         }
      }

      // Label-first overloads: for block lambdas whose source text would be too long or noisy to read as a label.

      ///<summary>Runs <paramref name="operation"/> as a timed span under the explicit <paramref name="label"/>, and returns its result.</summary>
      public T Time<T>(string label, Func<T> operation, [CallerMemberName] string caller = "")
         => @this.Time(operation, label, caller);

      ///<summary>Runs <paramref name="operation"/> as a timed span under the explicit <paramref name="label"/>.</summary>
      public Unit Time(string label, Action operation, [CallerMemberName] string caller = "")
         => @this.Time(operation, label, caller);

      ///<summary>Awaits <paramref name="operation"/> as a timed span under the explicit <paramref name="label"/>, and returns its result.</summary>
      public Task<T> TimeAsync<T>(string label, Func<Task<T>> operation, [CallerMemberName] string caller = "")
         => @this.TimeAsync(operation, label, caller);

      ///<summary>Awaits <paramref name="operation"/> as a timed span under the explicit <paramref name="label"/>.</summary>
      public Task TimeAsync(string label, Func<Task> operation, [CallerMemberName] string caller = "")
         => @this.TimeAsync(operation, label, caller);

      // Scope form: time the body of a using block.

      ///<summary>Opens a timed span under <paramref name="label"/>: logs "started" now and "took N.Nms" when the returned scope is disposed (the end of a <c>using</c> block).</summary>
      ///<remarks>Unlike the delegate overloads this cannot annotate a fault: a throw inside the <c>using</c> block still logs the elapsed time, but not which exception. Prefer the delegate overloads when that matters.</remarks>
      public IDisposable Time(string label, [CallerMemberName] string caller = "")
      {
         if(!@this.IsEnabled()) return DisabledTiming;
         var scope = TimedScope.Begin(@this, label, caller);
         return new Disposable(() => scope.End(fault: null));
      }
   }

   static readonly IDisposable DisabledTiming = new Disposable(() => {});
}
