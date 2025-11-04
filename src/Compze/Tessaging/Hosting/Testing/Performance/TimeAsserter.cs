using System;
using System.Threading;
using System.Threading.Tasks;
using Compze.Utilities.Contracts;
using Compze.Utilities.Logging;
using Compze.Utilities.SystemCE;
using JetBrains.Annotations;
using Compze.Utilities.SystemCE.ThreadingCE;

namespace Compze.Tessaging.Hosting.Testing.Performance;

static class TimeAsserter
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
      DeferredConsoleWriter.Execute(writer =>
         InternalExecute(() => StopwatchCE.TimeExecution(action, iterations), iterations, maxAverage, maxTotal, description, setup, tearDown, maxTries, writer));

   public static StopwatchCE.TimedThreadedExecutionSummary ExecuteThreaded([InstantHandle] Action action,
                                                                           int iterations = 1,
                                                                           TimeSpan? maxAverage = null,
                                                                           TimeSpan? maxTotal = null,
                                                                           string description = "",
                                                                           [InstantHandle] Action? setup = null,
                                                                           [InstantHandle] Action? tearDown = null,
                                                                           uint maxTries = MaxTriesDefault,
                                                                           int maxDegreeOfParallelism = -1) =>
      DeferredConsoleWriter.Execute(writer =>
         InternalExecute(() => StopwatchCE.TimeExecutionThreaded(action, iterations, maxDegreeOfParallelism), iterations, maxAverage, maxTotal, description, setup, tearDown, maxTries, writer));

   public static StopwatchCE.TimedExecutionSummary ExecuteThreadedLowOverhead([InstantHandle] Action action,
                                                                              int iterations = 1,
                                                                              TimeSpan? maxAverage = null,
                                                                              TimeSpan? maxTotal = null,
                                                                              string description = "",
                                                                              [InstantHandle] Action? setup = null,
                                                                              [InstantHandle] Action? tearDown = null,
                                                                              uint maxTries = MaxTriesDefault,
                                                                              int maxDegreeOfParallelism = -1) =>
      DeferredConsoleWriter.Execute(writer =>
         InternalExecute(() => StopwatchCE.TimeExecutionThreadedLowOverhead(action, iterations, maxDegreeOfParallelism), iterations, maxAverage, maxTotal, description, setup, tearDown, maxTries, writer));

   public static async Task<StopwatchCE.TimedExecutionSummary> ExecuteAsync([InstantHandle] Func<Task> action,
                                                                            int iterations = 1,
                                                                            TimeSpan? maxAverage = null,
                                                                            TimeSpan? maxTotal = null,
                                                                            string description = "",
                                                                            uint maxTries = MaxTriesDefault,
                                                                            [InstantHandle] Action? setup = null,
                                                                            [InstantHandle] Action? tearDown = null,
                                                                            [InstantHandle] Func<Task>? tearDownAsync = null) =>
      await DeferredConsoleWriter.ExecuteAsync(async writer =>
         await InternalExecuteAsync(() => StopwatchCE.TimeExecutionAsync(action, iterations), iterations, maxAverage, maxTotal, description, setup, tearDown, maxTries, tearDownAsync, writer));

   static TReturnValue InternalExecute<TReturnValue>([InstantHandle] Func<TReturnValue> runScenario,
                                                     int iterations,
                                                     TimeSpan? maxAverage,
                                                     TimeSpan? maxTotal,
                                                     string description,
                                                     [InstantHandle] Action? setup,
                                                     [InstantHandle] Action? tearDown,
                                                     uint maxTries,
                                                     DeferredConsoleWriter writer) where TReturnValue : StopwatchCE.TimedExecutionSummary =>
      InternalExecuteAsync(runScenario.AsAsync(), iterations, maxAverage, maxTotal, description, setup, tearDown, maxTries, null, writer).SyncResult();

   static async Task<TReturnValue> InternalExecuteAsync<TReturnValue>([InstantHandle] Func<Task<TReturnValue>> runScenario,
                                                                      int iterations,
                                                                      TimeSpan? maxAverage,
                                                                      TimeSpan? maxTotal,
                                                                      string description,
                                                                      [InstantHandle] Action? setup,
                                                                      [InstantHandle] Action? tearDown,
                                                                      uint maxTries,
                                                                      [InstantHandle] Func<Task>? tearDownAsync,
                                                                      DeferredConsoleWriter writer) where TReturnValue : StopwatchCE.TimedExecutionSummary
   {
      Assert.Argument.Is(maxTries > 0);
      maxAverage = TestEnv.Performance.AdjustForMachineSlowness(maxAverage);
      maxTotal = TestEnv.Performance.AdjustForMachineSlowness(maxTotal);
      TestEnv.Performance.LogMachineSlownessAdjustment();
      maxTries = Math.Min(MaxTriesLimit, maxTries);

      writer.WriteLine();
      writer.WriteImportantLine($"""
                                    "{description}" {iterations:### ### ###} {iterations.Pluralize("iteration")} starting
                                    """);

      for(var tries = 1; tries <= maxTries; tries++)
      {
         setup?.Invoke();

         try
         {
            var executionSummary = await runScenario();

            var failureTessage = GetFailureTessage(executionSummary, maxAverage, maxTotal);
            if(failureTessage.Length > 0)
            {
               if(tries >= maxTries) throw new TimeOutException($"""
                                                                 {description}:
                                                                 {failureTessage.Indent()}
                                                                 """);
               var waitTime = Math.Min(Math.Pow(2, tries), 50) * 10.Milliseconds(); //Back off on retries exponentially starting with 10ms, but only up to a maximum wait time of .5 seconds between retries.
               writer.WriteWarningLine($"Try: {tries} {failureTessage}, waiting {waitTime.FormatReadable()} before next attempt");
               if(VerboseMode)
                  Log.Warning($"{description}: Try: {tries} {failureTessage}, waiting {waitTime.FormatReadable()} before next attempt");
               Thread.Sleep(waitTime);
               continue;
            }

            PrintSummary(executionSummary, iterations, maxAverage, maxTotal, writer);
            writer.WriteImportantLine("DONE");
            writer.WriteLine();
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

   static void PrintSummary(StopwatchCE.TimedExecutionSummary executionSummary, int iterations, TimeSpan? maxAverage, TimeSpan? maxTotal, DeferredConsoleWriter writer)
   {
      var maxAverageReport = maxAverage == null
                                ? ""
                                : $" {Percent(executionSummary.Average, maxAverage.Value)} of {nameof(maxAverage)}: {maxAverage.FormatReadable()}";

      var maxTotalReport = maxTotal == null
                              ? ""
                              : $" {Percent(executionSummary.Total, maxTotal.Value)} of {nameof(maxTotal)}: {maxTotal.FormatReadable()}";

      if(iterations > 1)
      {
         writer.WriteLine($"""

                                 Total:   {executionSummary.Total.FormatReadable()} {maxTotalReport}
                                 Average: {executionSummary.Average.FormatReadable()} {maxAverageReport}
                                 """
                               .RemoveLeadingLineBreak());
      } else
      {
         writer.WriteLine($"Total:   {executionSummary.Total.FormatReadable()} {maxTotalReport} ");
      }

      if(executionSummary is StopwatchCE.TimedThreadedExecutionSummary threadedSummary)
      {
         writer.WriteLine($"""
                                
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