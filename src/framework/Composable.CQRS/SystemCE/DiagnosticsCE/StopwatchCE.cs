using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.TasksCE;
using JetBrains.Annotations;

namespace Composable.SystemCE.DiagnosticsCE;

///<summary>Extensions to the Stopwatch class and related functionality.</summary>
public static class StopwatchCE
{
   internal static TimingsStatisticsCollector CreateCollector(string name, params TimeSpan[] rangesToCollect) => new(name, rangesToCollect);

   ///<summary>Measures how long it takes to execute <paramref name="action"/></summary>
   internal static TimeSpan TimeExecution([InstantHandle] Action action) => new Stopwatch().TimeExecution(action);

   internal static async Task<TimeSpan> TimeExecutionAsync([InstantHandle] Func<Task> action) => await new Stopwatch().TimeExecutionAsync(action).CaF();

   ///<summary>Measures how long it takes to execute <paramref name="action"/></summary>
   static TimeSpan TimeExecution(this Stopwatch @this, [InstantHandle] Action action)
   {
      @this.Reset();
      @this.Start();
      action();
      return @this.Elapsed;
   }

   ///<summary>Measures how long it takes to execute <paramref name="action"/></summary>
   static async Task<TimeSpan> TimeExecutionAsync(this Stopwatch @this, [InstantHandle] Func<Task> action)
   {
      @this.Reset();
      @this.Start();
      await action().CaF();
      return @this.Elapsed;
   }

   // ReSharper disable once MethodOverloadWithOptionalParameter
   public static async Task<TimedExecutionSummary> TimeExecutionAsync([InstantHandle] Func<Task> action, int iterations = 1)
   {
      var total = await TimeExecutionAsync(
                     async () =>
                     {
                        for(var i = 0; i < iterations; i++)
                        {
                           await action().CaF();
                        }
                     }).CaF();

      return new TimedExecutionSummary(iterations, total);
   }

   // ReSharper disable once MethodOverloadWithOptionalParameter
   public static TimedExecutionSummary TimeExecution([InstantHandle] Action action, int iterations = 1)
   {
      var total = TimeExecution(
         () =>
         {
            for(var i = 0; i < iterations; i++)
            {
               action();
            }
         });

      return new TimedExecutionSummary(iterations, total);
   }

   public static TimedExecutionSummary TimeExecutionThreadedLowOverhead([InstantHandle] Action action, int iterations = 1, int maxDegreeOfParallelism = -1)
   {
      maxDegreeOfParallelism = maxDegreeOfParallelism == -1
                                  ? Math.Max(Environment.ProcessorCount / 2, 4)
                                  : maxDegreeOfParallelism;

      maxDegreeOfParallelism = Math.Min(maxDegreeOfParallelism, iterations);

      //Profiling shows that the max time for this line to execute is quite high. So it should be improving consistency of measurements significantly as compared to taking the hit during timing.
      ThreadPoolCE.TryToEnsureSufficientIdleThreadsToRunTasksConcurrently(maxDegreeOfParallelism);

      var total = TimeExecution(
         () => Parallel.For(fromInclusive: 0,
                            toExclusive: iterations,
                            body: _ => action(),
                            parallelOptions: new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism }));

      return new TimedExecutionSummary(iterations, total);
   }

   public static TimedThreadedExecutionSummary TimeExecutionThreaded([InstantHandle] Action action, int iterations = 1, int maxDegreeOfParallelism = -1)
   {
      maxDegreeOfParallelism = maxDegreeOfParallelism == -1
                                  ? Math.Max(Environment.ProcessorCount / 2, 4)
                                  : maxDegreeOfParallelism;

      maxDegreeOfParallelism = Math.Min(maxDegreeOfParallelism, iterations);

      var individual = new ConcurrentStack<TimeSpan>();

      //Profiling shows that the max time for this line to execute is quite high. So it should be improving consistency of measurements significantly as compared to taking the hit during timing.
      ThreadPoolCE.TryToEnsureSufficientIdleThreadsToRunTasksConcurrently(maxDegreeOfParallelism);

      var total = TimeExecution(
         () => Parallel.For(fromInclusive: 0,
                            toExclusive: iterations,
                            body: _ =>
                            {
                               var timing = TimedAction();
                               individual.Push(timing);
                            },
                            parallelOptions: new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism }));

      return new TimedThreadedExecutionSummary(iterations, individual.ToList(), total);

      TimeSpan TimedAction() => TimeExecution(action);
   }

   public class TimedExecutionSummary(int iterations, TimeSpan total)
   {
      int Iterations { get; } = iterations;
      public TimeSpan Total { get; } = total;
      public TimeSpan Average => (Total.TotalMilliseconds / Iterations).Milliseconds();
   }

   public class TimedThreadedExecutionSummary(int iterations, IReadOnlyList<TimeSpan> individualExecutionTimes, TimeSpan total, string description = "")
      : TimedExecutionSummary(iterations, total)
   {
      readonly string _description = description;

      public IReadOnlyList<TimeSpan> IndividualExecutionTimes { get; } = individualExecutionTimes;

      public override string ToString() => $@"
{_description}
Total: {Total.FormatReadable()}
Average: {Total.FormatReadable()}

Individual execution times    
    Average: {IndividualExecutionTimes.Average().FormatReadable()}
    Min:     {IndividualExecutionTimes.Min().FormatReadable()}
    Max:     {IndividualExecutionTimes.Max().FormatReadable()}
    Sum:     {IndividualExecutionTimes.Sum().FormatReadable()}
";
   }
}
