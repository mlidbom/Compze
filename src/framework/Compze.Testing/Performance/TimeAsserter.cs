using System;
using System.Threading;
using System.Threading.Tasks;
using Compze.Contracts;
using Compze.Logging;
using Compze.SystemCE;
using Compze.SystemCE.ThreadingCE;
using JetBrains.Annotations;

namespace Compze.Testing.Performance;

static class TimeAsserter
{
   const int MaxTriesLimit = 40;
   const int MaxTriesDefault = 10;

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

   static TReturnValue InternalExecute<TReturnValue>([InstantHandle] Func<TReturnValue> runScenario,
                                                     int iterations,
                                                     TimeSpan? maxAverage,
                                                     TimeSpan? maxTotal,
                                                     string description,
                                                     [InstantHandle] Action? setup,
                                                     [InstantHandle] Action? tearDown,
                                                     uint maxTries = MaxTriesDefault) where TReturnValue : StopwatchCE.TimedExecutionSummary =>
      InternalExecuteAsync(runScenario.AsAsync(), iterations, maxAverage, maxTotal, description, setup, tearDown, maxTries).SyncResult();

   static async Task<TReturnValue> InternalExecuteAsync<TReturnValue>([InstantHandle] Func<Task<TReturnValue>> runScenario,
                                                                      int iterations,
                                                                      TimeSpan? maxAverage,
                                                                      TimeSpan? maxTotal,
                                                                      string description,
                                                                      [InstantHandle] Action? setup,
                                                                      [InstantHandle] Action? tearDown,
                                                                      uint maxTries = MaxTriesDefault,
                                                                      [InstantHandle] Func<Task>? tearDownAsync = null) where TReturnValue : StopwatchCE.TimedExecutionSummary
   {
      Assert.Argument.Is(maxTries > 0);
      maxAverage = TestEnv.Performance.AdjustForMachineSlowness(maxAverage);
      maxTotal = TestEnv.Performance.AdjustForMachineSlowness(maxTotal);
      TestEnv.Performance.LogMachineSlownessAdjustment();
      maxTries = Math.Min(MaxTriesLimit, maxTries);

      ConsoleCE.WriteLine();
      ConsoleCE.WriteImportantLine($"""
                                    "{description}" {iterations:### ### ###} {iterations.Pluralize("iteration")} starting
                                    """);

      for(var tries = 1; tries <= maxTries; tries++)
      {
         setup?.Invoke();

         try
         {
            var executionSummary = await runScenario();

            var failureMessage = GetFailureMessage(executionSummary, maxAverage, maxTotal);
            if(failureMessage.Length > 0)
            {
               if(tries >= maxTries) throw new TimeOutException(failureMessage);
               var waitTime = Math.Min(Math.Pow(2, tries), 50) * 10.Milliseconds(); //Back off on retries exponentially starting with 10ms, but only up to a maximum wait time of .5 seconds between retries.
               ConsoleCE.WriteWarningLine($"Try: {tries} {failureMessage}, waiting {waitTime.FormatReadable()} before next attempt");
               Thread.Sleep(waitTime);
               continue;
            }

            PrintSummary(executionSummary, iterations, maxAverage, maxTotal);
            ConsoleCE.WriteImportantLine("DONE");
            ConsoleCE.WriteLine();
            return executionSummary;
         }
         finally
         {
            if(tearDownAsync != null)
            {
               await tearDownAsync();
            } else
            {
               tearDown?.Invoke();
            }
         }
      }

      throw new Exception("Unreachable");
   }

   static string GetFailureMessage(StopwatchCE.TimedExecutionSummary executionSummary, TimeSpan? maxAverage, TimeSpan? maxTotal)
   {
      var failureMessage = "";
      if(maxTotal.HasValue && executionSummary.Total > maxTotal.Value)
      {
         failureMessage = $"Total:{executionSummary.Total.FormatReadable()} {Percent(executionSummary.Total, maxTotal.Value)} of {nameof(maxTotal)}: {maxTotal.FormatReadable()}";
      } else if(maxAverage.HasValue && executionSummary.Average > maxAverage.Value)
      {
         failureMessage = $" {Percent(executionSummary.Average, maxAverage.Value)} of {nameof(maxAverage)}: {maxAverage.FormatReadable()}";
      }

      return failureMessage;
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
         ConsoleCE.WriteLine($"""

                                 Total:   {executionSummary.Total.FormatReadable()} {maxTotalReport}
                                 Average: {executionSummary.Average.FormatReadable()} {maxAverageReport}
                                 """
                               .RemoveLeadingLineBreak());
      } else
      {
         ConsoleCE.WriteLine($"Total:   {executionSummary.Total.FormatReadable()} {maxTotalReport} ");
      }

      if(executionSummary is StopwatchCE.TimedThreadedExecutionSummary threadedSummary)
      {
         ConsoleCE.WriteLine($"""
                                
                              Individual execution times    
                                  Average: {threadedSummary.IndividualExecutionTimes.Average().FormatReadable()}
                                  Min:     {threadedSummary.IndividualExecutionTimes.Min().FormatReadable()}
                                  Max:     {threadedSummary.IndividualExecutionTimes.Max().FormatReadable()}
                                  Sum:     {threadedSummary.IndividualExecutionTimes.Sum().FormatReadable()}

                              """);
      }
   }

   static string Percent(TimeSpan percent, TimeSpan of) => $"{(int)(percent.TotalMilliseconds / of.TotalMilliseconds * 100)}%";

   public class TimeOutException(string message) : Exception(message);
}