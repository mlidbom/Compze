using Compze.Contracts;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using JetBrains.Annotations;
using Compze.Threading;
using Compze.Threading.Interprocess;

namespace Compze.Internals.Testing.Performance;

public static partial class TimeAsserter
{
   const int MaxTriesLimit = 40;
   const int MaxTriesDefault = 10;

   static ILogger Log => CompzeLogger.For(typeof(TimeAsserter));

   public static StopwatchCE.TimedExecutionSummary Execute([InstantHandle] Action action,
                                                           int iterations = 1,
                                                           TimeSpan? maxAverage = null,
                                                           TimeSpan? maxTotal = null,
                                                           string description = "",
                                                           uint maxTries = MaxTriesDefault,
                                                           [InstantHandle] Action? setup = null,
                                                           [InstantHandle] Action? tearDown = null) =>
      InternalExecute(() => StopwatchCE.TimeExecution(action, iterations), iterations, maxAverage, maxTotal, description, setup, tearDown, maxTries);

   public static StopwatchCE.TimedThreadedExecutionSummary ExecuteThreaded([InstantHandle] Action action,
                                                                           int iterations = 1,
                                                                           TimeSpan? maxAverage = null,
                                                                           TimeSpan? maxTotal = null,
                                                                           string description = "",
                                                                           [InstantHandle] Action? setup = null,
                                                                           [InstantHandle] Action? tearDown = null,
                                                                           uint maxTries = MaxTriesDefault,
                                                                           int maxDegreeOfParallelism = -1) =>
      InternalExecute(() => StopwatchCE.TimeExecutionThreaded(action, iterations, maxDegreeOfParallelism), iterations, maxAverage, maxTotal, description, setup, tearDown, maxTries);

   public static StopwatchCE.TimedExecutionSummary ExecuteThreadedLowOverhead([InstantHandle] Action action,
                                                                              int iterations = 1,
                                                                              TimeSpan? maxAverage = null,
                                                                              TimeSpan? maxTotal = null,
                                                                              string description = "",
                                                                              [InstantHandle] Action? setup = null,
                                                                              [InstantHandle] Action? tearDown = null,
                                                                              uint maxTries = MaxTriesDefault,
                                                                              int maxDegreeOfParallelism = -1) =>
      InternalExecute(() => StopwatchCE.TimeExecutionThreadedLowOverhead(action, iterations, maxDegreeOfParallelism), iterations, maxAverage, maxTotal, description, setup, tearDown, maxTries);

   public static async Task<StopwatchCE.TimedExecutionSummary> ExecuteAsync([InstantHandle] Func<Task> action,
                                                                            int iterations = 1,
                                                                            TimeSpan? maxAverage = null,
                                                                            TimeSpan? maxTotal = null,
                                                                            string description = "",
                                                                            uint maxTries = MaxTriesDefault,
                                                                            [InstantHandle] Action? setup = null,
                                                                            [InstantHandle] Action? tearDown = null,
                                                                            [InstantHandle] Func<Task>? tearDownAsync = null) =>
      await InternalExecuteAsync(() => StopwatchCE.TimeExecutionAsync(action, iterations), iterations, maxAverage, maxTotal, description, setup, tearDown, maxTries, tearDownAsync).caf();

   ///<summary>Performance tests take turns, across every process on the machine: timings measured beside another performance test would measure<br/>
   /// the two of them.</summary>
   static readonly IProcessLock PerformanceTestTurn = IProcessLock.New("Compze.TimeAsserter.PerformanceTestLock", Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "Compze")));

   ///<summary>How long a performance test waits for its turn: others may be queued ahead of it, each trying up to <see cref="MaxTriesLimit"/> times.</summary>
   static readonly WaitTimeout HowLongToWaitForTheTurn = WaitTimeout.Minutes(5);

   static TReturnValue InternalExecute<TReturnValue>([InstantHandle] Func<TReturnValue> runScenario,
                                                     int iterations,
                                                     TimeSpan? maxAverage,
                                                     TimeSpan? maxTotal,
                                                     string description,
                                                     [InstantHandle] Action? setup,
                                                     [InstantHandle] Action? tearDown,
                                                     uint maxTries) where TReturnValue : StopwatchCE.TimedExecutionSummary
   {
      using(PerformanceTestTurn.Take(TurnPurpose(description), HowLongToWaitForTheTurn))
      {
         var assertion = TimeAssertion.Start(iterations, maxAverage, maxTotal, description, maxTries);
         for(var tries = 1;; tries++)
         {
            setup?.Invoke();
            try
            {
               var executionSummary = runScenario();
               if(assertion.Passed(executionSummary, tries)) return executionSummary;
            }
            finally
            {
               tearDown?.Invoke();
            }

            Thread.Sleep(assertion.PauseAfterFailedTry(tries));
         }
      }
   }

   static async Task<TReturnValue> InternalExecuteAsync<TReturnValue>([InstantHandle] Func<Task<TReturnValue>> runScenario,
                                                                      int iterations,
                                                                      TimeSpan? maxAverage,
                                                                      TimeSpan? maxTotal,
                                                                      string description,
                                                                      [InstantHandle] Action? setup,
                                                                      [InstantHandle] Action? tearDown,
                                                                      uint maxTries,
                                                                      [InstantHandle] Func<Task>? tearDownAsync) where TReturnValue : StopwatchCE.TimedExecutionSummary
   {
      using(await PerformanceTestTurn.TakeAsync(TurnPurpose(description), HowLongToWaitForTheTurn).caf())
      {
         var assertion = TimeAssertion.Start(iterations, maxAverage, maxTotal, description, maxTries);
         for(var tries = 1;; tries++)
         {
            setup?.Invoke();
            try
            {
               var executionSummary = await runScenario().caf();
               if(assertion.Passed(executionSummary, tries)) return executionSummary;
            }
            finally
            {
               if(tearDownAsync != null) await tearDownAsync().caf();
               else tearDown?.Invoke();
            }

            await Task.Delay(assertion.PauseAfterFailedTry(tries)).caf();
         }
      }
   }

   static string TurnPurpose(string description) => $"the performance test \"{description}\"";
}
