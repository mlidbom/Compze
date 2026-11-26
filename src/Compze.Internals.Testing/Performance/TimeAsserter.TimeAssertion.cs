using Compze.Contracts;
using Compze.Internals.SystemCE;

namespace Compze.Internals.Testing.Performance;

public static partial class TimeAsserter
{
   ///<summary>What one <see cref="TimeAsserter"/> call asserts of a scenario's timings: the limits they must keep within, adjusted for the machine,<br/>
   /// how many tries the scenario gets to keep within them, and the log of how each try went. The synchronous and the async execution share it,<br/>
   /// each running the tries its own way.</summary>
   sealed class TimeAssertion
   {
      readonly int _iterations;
      readonly TimeSpan? _maxAverage;
      readonly TimeSpan? _maxTotal;
      readonly string _description;
      readonly uint _maxTries;

      TimeAssertion(int iterations, TimeSpan? maxAverage, TimeSpan? maxTotal, string description, uint maxTries)
      {
         _iterations = iterations;
         _maxAverage = maxAverage;
         _maxTotal = maxTotal;
         _description = description;
         _maxTries = maxTries;
      }

      ///<summary>The assertion, with its limits adjusted for the machine, or dropped when performance tests run as stress tests only, once the start of<br/>
      /// the scenario is logged.</summary>
      internal static TimeAssertion Start(int iterations, TimeSpan? maxAverage, TimeSpan? maxTotal, string description, uint maxTries)
      {
         Contract.Argument.Assert(maxTries > 0);
         if(TestEnv.Performance.StressTestOnly)
         {
            maxAverage = null;
            maxTotal = null;
         }
         else
         {
            maxAverage = TestEnv.Performance.AdjustForMachineSlowness(maxAverage);
            maxTotal = TestEnv.Performance.AdjustForMachineSlowness(maxTotal);
         }
         TestEnv.Performance.LogPerformanceTestConfiguration();

         Log.Info($"""

                  ############################## "{description}" {iterations:### ### ###} {iterations.Pluralize("iteration")} starting
                  """);

         return new TimeAssertion(iterations, maxAverage, maxTotal, description, Math.Min(MaxTriesLimit, maxTries));
      }

      ///<summary>True if the timings of try number <paramref name="tries"/>, <paramref name="executionSummary"/>, keep within the limits, which it then<br/>
      /// logs; false if they don't, and another try is due. Throws if they don't on the last try.</summary>
      internal bool Passed(StopwatchCE.TimedExecutionSummary executionSummary, int tries)
      {
         var failureTessage = GetFailureTessage(executionSummary);
         if(failureTessage.Length > 0)
         {
            if(tries >= _maxTries)
               throw new TimeOutException($"""
                                           {_description}:
                                           {failureTessage.Indent()}
                                           """);
            Log.Warning($"Try: {tries} {failureTessage}");
            return false;
         }

         PrintSummary(executionSummary);
         Log.Info("""
                  ############################## DONE

                  """);
         return true;
      }

      ///<summary>How long to wait before the next try, after try number <paramref name="tries"/> failed: exponentially longer, starting with 10 milliseconds,<br/>
      /// up to half a second.</summary>
      internal TimeSpan PauseAfterFailedTry(int tries)
      {
         var waitTime = Math.Min(Math.Pow(2, tries), 50) * 10.Milliseconds();
         Log.Warning($"Waiting {waitTime.FormatReadable()} before try {tries + 1}");
         return waitTime;
      }

      string GetFailureTessage(StopwatchCE.TimedExecutionSummary executionSummary)
      {
         var failureTessage = "";
         if(_maxTotal.HasValue && executionSummary.Total > _maxTotal.Value)
         {
            failureTessage = $"Total:{executionSummary.Total.FormatReadable()} {Percent(executionSummary.Total, _maxTotal.Value)} of maxTotal: {_maxTotal.FormatReadable()}";
         } else if(_maxAverage.HasValue && executionSummary.Average > _maxAverage.Value)
         {
            failureTessage = $" {Percent(executionSummary.Average, _maxAverage.Value)} of maxAverage: {_maxAverage.FormatReadable()}";
         }

         return failureTessage;
      }

      void PrintSummary(StopwatchCE.TimedExecutionSummary executionSummary)
      {
         var maxAverageReport = _maxAverage == null
                                   ? ""
                                   : $" {Percent(executionSummary.Average, _maxAverage.Value)} of maxAverage: {_maxAverage.FormatReadable()}";

         var maxTotalReport = _maxTotal == null
                                 ? ""
                                 : $" {Percent(executionSummary.Total, _maxTotal.Value)} of maxTotal: {_maxTotal.FormatReadable()}";

         Log.Info(_iterations > 1
                     ? $"""

                           Total:   {executionSummary.Total.FormatReadable()} {maxTotalReport}
                           Average: {executionSummary.Average.FormatReadable()} {maxAverageReport}
                        """
                     : $"Total:   {executionSummary.Total.FormatReadable()} {maxTotalReport} ");

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
}
