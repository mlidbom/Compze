using System.Runtime.CompilerServices;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.SystemCE;
using Compze.Internals.Logging._private;

// ReSharper disable UnusedMember.Global Diagnostics API — used occasionally while researching performance, and by sibling apps built on our logging.
// ReSharper disable UnusedType.Global

namespace Compze.Internals.Logging;

///<summary>
/// Convenience instrumentation on top of <see cref="ILevelLogger"/> for logging how long things take.
/// <c>ExecutionTime</c> times a region — a delegate, or a <c>using</c> scope — labelled with the region's own
/// source text. <c>MethodExecutionTime</c> times the enclosing method as a whole, labelled with the method's
/// name; wrap the entire body (an expression-bodied lambda, or a <c>using</c> as the first statement) so the
/// label tells the truth. Each timed span logs two lines: "started" when entered and "took N.Nms" when it ends,
/// both prefixed with the span's ancestry path (<c>#root/#parent/#self</c>) so the pair is matched and its parent
/// identified even when many spans interleave across threads and awaits. The duration is captured as a structured
/// <c>elapsedMs</c> property, so it can be aggregated, not just read. When the level is disabled the delegate runs
/// with zero added cost: no stopwatch, no allocation, no lines.
///</summary>
///<remarks>
/// Cache the level logger in a field named for the verb it supplies, and call these on it:
/// <c>static readonly ILevelLogger Log = For&lt;X&gt;().Debug();</c> then <c>Log.ExecutionTime(() =&gt; Decode(path))</c>,
/// or, to time a whole method without changing a line of its body:
/// <code>
/// void Render() => Log.MethodExecutionTime(() =>
/// {
///    Compose();
///    Present();
/// });
/// </code>
/// Caching is safe — whether the level is enabled is still decided live on each call, not frozen when the field is
/// built — and it avoids re-allocating the level-logger wrapper.
///</remarks>
public static class LevelLoggerExtensions
{
   extension(ILevelLogger @this)
   {
      // ── Time a region: labelled with the operation's own source text ──

      ///<summary>Runs <paramref name="operation"/> as a timed span labelled with its own source text, and returns its result.</summary>
      public T ExecutionTime<T>(Func<T> operation,
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
      public Unit ExecutionTime(Action operation,
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
      public async Task<T> ExecutionTimeAsync<T>(Func<Task<T>> operation,
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
      public async Task ExecutionTimeAsync(Func<Task> operation,
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
      public T ExecutionTime<T>(string label, Func<T> operation, [CallerMemberName] string caller = "")
         => @this.ExecutionTime(operation, label, caller);

      ///<summary>Runs <paramref name="operation"/> as a timed span under the explicit <paramref name="label"/>.</summary>
      public Unit ExecutionTime(string label, Action operation, [CallerMemberName] string caller = "")
         => @this.ExecutionTime(operation, label, caller);

      ///<summary>Awaits <paramref name="operation"/> as a timed span under the explicit <paramref name="label"/>, and returns its result.</summary>
      public Task<T> ExecutionTimeAsync<T>(string label, Func<Task<T>> operation, [CallerMemberName] string caller = "")
         => @this.ExecutionTimeAsync(operation, label, caller);

      ///<summary>Awaits <paramref name="operation"/> as a timed span under the explicit <paramref name="label"/>.</summary>
      public Task ExecutionTimeAsync(string label, Func<Task> operation, [CallerMemberName] string caller = "")
         => @this.ExecutionTimeAsync(operation, label, caller);

      // Scope form: time the body of a using block.

      ///<summary>Opens a timed span under <paramref name="label"/>: logs "started" now and "took N.Nms" when the returned scope is disposed (the end of a <c>using</c> block).</summary>
      ///<remarks>Unlike the delegate overloads this cannot annotate a fault: a throw inside the <c>using</c> block still logs the elapsed time, but not which exception. Prefer the delegate overloads when that matters.</remarks>
      public IDisposable ExecutionTime(string label, [CallerMemberName] string caller = "")
      {
         if(!@this.IsEnabled()) return DisabledTiming;
         var scope = TimedScope.Begin(@this, label, caller);
         return new Disposable(() => scope.End(fault: null));
      }

      // ── Time the whole enclosing method: labelled with the method's name ──

      ///<summary>Runs <paramref name="body"/> — the enclosing method's entire body — as a timed span labelled with the method's name, and returns its result.</summary>
      public T MethodExecutionTime<T>(Func<T> body, [CallerMemberName] string method = "")
         => @this.ExecutionTime(body, label: method, caller: method);

      ///<summary>Runs <paramref name="body"/> — the enclosing method's entire body — as a timed span labelled with the method's name.</summary>
      public Unit MethodExecutionTime(Action body, [CallerMemberName] string method = "")
         => @this.ExecutionTime(body, label: method, caller: method);

      ///<summary>Awaits <paramref name="body"/> — the enclosing method's entire body — as a timed span labelled with the method's name, and returns its result.</summary>
      public Task<T> MethodExecutionTimeAsync<T>(Func<Task<T>> body, [CallerMemberName] string method = "")
         => @this.ExecutionTimeAsync(body, label: method, caller: method);

      ///<summary>Awaits <paramref name="body"/> — the enclosing method's entire body — as a timed span labelled with the method's name.</summary>
      public Task MethodExecutionTimeAsync(Func<Task> body, [CallerMemberName] string method = "")
         => @this.ExecutionTimeAsync(body, label: method, caller: method);

      ///<summary>Opens a timed span for the enclosing method, labelled with its name: logs "started" now and "took N.Nms" when the returned scope is disposed. Place it as the method's first statement (<c>using var _ = Log.MethodExecutionTime();</c>) so the scope covers the whole body.</summary>
      ///<remarks>Like the region scope form this cannot annotate a fault. Prefer the delegate overload when that matters.</remarks>
      public IDisposable MethodExecutionTime([CallerMemberName] string method = "")
         => @this.ExecutionTime(method, caller: method);
   }

   static readonly IDisposable DisabledTiming = new Disposable(() => {});
}
