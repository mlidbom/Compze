using System;
using System.Threading;
using System.Threading.Tasks;
using Compze.Contracts;
using Compze.Utilities.Logging;
using Compze.Utilities.SystemCE;
using JetBrains.Annotations;
using Compze.Threading;
using Compze.Threading.TasksCE;

namespace Compze.Tessaging.Hosting.Testing.Performance;

public static class TimeAsserter
{
   const int MaxTriesLimit = 40;
   const int MaxTriesDefault = 10;

   public static bool VerboseMode { get; set; } = false;
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
      await InternalExecuteAsync(() => StopwatchCE.TimeExecutionAsync(action, iterations), iterations, maxAverage, maxTotal, description, setup, tearDown, maxTries, tearDownAsync);

   static readonly MutexCE Mutex = MutexCE.ForMutexNamed("Compze.TimeAsserter.PerformanceTestLock");

   static TReturnValue InternalExecute<TReturnValue>([InstantHandle] Func<TReturnValue> runScenario,
                                                     int iterations,
                                                     TimeSpan? maxAverage,
                                                     TimeSpan? maxTotal,
                                                     string description,
                                                     [InstantHandle] Action? setup,
                                                     [InstantHandle] Action? tearDown,
                                                     uint maxTries) where TReturnValue : StopwatchCE.TimedExecutionSummary =>
      Mutex.Locked(() => RunScenarioWithRetries(runScenario, iterations, maxAverage, maxTotal, description, setup, tearDown, maxTries));

   static async Task<TReturnValue> InternalExecuteAsync<TReturnValue>([InstantHandle] Func<Task<TReturnValue>> runScenario,
                                                                      int iterations,
                                                                      TimeSpan? maxAverage,
                                                                      TimeSpan? maxTotal,
                                                                      string description,
                                                                      [InstantHandle] Action? setup,
                                                                      [InstantHandle] Action? tearDown,
                                                                      uint maxTries,
                                                                      [InstantHandle] Func<Task>? tearDownAsync) where TReturnValue : StopwatchCE.TimedExecutionSummary =>
      await Task.Run(() => Mutex.Locked(() => RunScenarioWithRetries(() => runScenario().ResultUnwrappingException(), iterations, maxAverage, maxTotal, description, setup, () =>
      {
         if(tearDownAsync != null)
            tearDownAsync().WaitUnwrappingException();
         else
            tearDown?.Invoke();
      }, maxTries)));

   static TReturnValue RunScenarioWithRetries<TReturnValue>([InstantHandle] Func<TReturnValue> runScenario,
                                                            int iterations,
                                                            TimeSpan? maxAverage,
                                                            TimeSpan? maxTotal,
                                                            string description,
                                                            [InstantHandle] Action? setup,
                                                            [InstantHandle] Action? tearDown,
                                                            uint maxTries) where TReturnValue : StopwatchCE.TimedExecutionSummary
   {
      Contract.Argument.Assert(maxTries > 0);
      maxAverage = TestEnv.Performance.AdjustForMachineSlowness(maxAverage);
      maxTotal = TestEnv.Performance.AdjustForMachineSlowness(maxTotal);
      TestEnv.Performance.LogMachineSlownessAdjustment();
      maxTries = Math.Min(MaxTriesLimit, maxTries);

      Log.Info($"""
               
               ############################## "{description}" {iterations:### ### ###} {iterations.Pluralize("iteration")} starting
               """);

      for(var tries = 1; tries <= maxTries; tries++)
      {
         setup?.Invoke();

         try
         {
            var executionSummary = runScenario();

            var failureTessage = GetFailureTessage(executionSummary, maxAverage, maxTotal);
            if(failureTessage.Length > 0)
            {
               if(tries >= maxTries)
                  throw new TimeOutException($"""
                                              {description}:
                                              {failureTessage.Indent()}
                                              """);
               var waitTime = Math.Min(Math.Pow(2, tries), 50) * 10.Milliseconds(); //Back off on retries exponentially starting with 10ms, but only up to a maximum wait time of .5 seconds between retries.
               Log.Warning($"Try: {tries} {failureTessage}, waiting {waitTime.FormatReadable()} before next attempt");
               Thread.Sleep(waitTime);
               continue;
            }

            PrintSummary(executionSummary, iterations, maxAverage, maxTotal);
            Log.Info("""
                     ############################## DONE

                     """);
            return executionSummary;
         }
         finally
         {
            tearDown?.Invoke();
         }
      }

      throw new Exception("Unreachable");
   }

   static string GetFailureTessage(StopwatchCE.TimedExecutionSummary executionSummary, TimeSpan? maxAverage, TimeSpan? maxTotal)
   {
      var failureTessage = "";
      if(maxTotal.HasValue && executionSummary.Total > maxTotal.Value)
      {
         failureTessage = $"Total:{executionSummary.Total.FormatReadable()} {Percent(executionSummary.Total, maxTotal.Value)} of {nameof(maxTotal)}: {maxTotal.FormatReadable()}";
      } else if(maxAverage.HasValue && executionSummary.Average > maxAverage.Value)
      {
         failureTessage = $" {Percent(executionSummary.Average, maxAverage.Value)} of {nameof(maxAverage)}: {maxAverage.FormatReadable()}";
      }

      return failureTessage;
   }

   static void PrintSummary(StopwatchCE.TimedExecutionSummary executionSummary, int iterations, TimeSpan? maxAverage, TimeSpan? maxTotal)
   {
      var maxAverageReport = maxAverage == null
                                ? ""
                                : $" {Percent(executionSummary.Average, maxAverage.Value)} of {nameof(maxAverage)}: {maxAverage.FormatReadable()}";

      var maxTotalReport = maxTotal == null
                              ? ""
                              : $" {Percent(executionSummary.Total, maxTotal.Value)} of {nameof(maxTotal)}: {maxTotal.FormatReadable()}";

      if(iterations > 1)
      {
         Log.Info($"""

                      Total:   {executionSummary.Total.FormatReadable()} {maxTotalReport}
                      Average: {executionSummary.Average.FormatReadable()} {maxAverageReport}
                   """);
      } else
      {
         Log.Info($"Total:   {executionSummary.Total.FormatReadable()} {maxTotalReport} ");
      }

      if(executionSummary is StopwatchCE.TimedThreadedExecutionSummary threadedSummary)
      {
         Log.Info($"""
                    
                   Individual execution times    
                       Average: {threadedSummary.IndividualExecutionTimes.Average().FormatReadable()}
                       Min:     {threadedSummary.IndividualExecutionTimes.Min().FormatReadable()}
                       Max:     {threadedSummary.IndividualExecutionTimes.Max().FormatReadable()}
                       Sum:     {threadedSummary.IndividualExecutionTimes.Sum().FormatReadable()}

                   """);
      }
   }

   static string Percent(TimeSpan percent, TimeSpan of) => $"{(int)(percent.TotalMilliseconds / of.TotalMilliseconds * 100)}%";

   class TimeOutException(string message) : Exception(message);
}
