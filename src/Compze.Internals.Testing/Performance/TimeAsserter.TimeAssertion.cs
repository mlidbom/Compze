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
      int _tries;

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
         TestEnv.Performance.LogPerformanceTestConfiguration();

         Log.Info($"""

                  ############################## "{description}" {iterations:### ### ###} {iterations.Pluralize("iteration")} starting
                  """);

         return new TimeAssertion(iterations, LimitOnThisMachine(maxAverage), LimitOnThisMachine(maxTotal), description, Math.Min(MaxTriesLimit, maxTries));

         static TimeSpan? LimitOnThisMachine(TimeSpan? limit) => TestEnv.Performance.StressTestOnly ? null : TestEnv.Performance.AdjustForMachineSlowness(limit);
      }

      ///<summary>True if the timings of the scenario's next try, <paramref name="executionSummary"/>, keep within the limits, which it then logs: false<br/>
      /// if they don't, and another try is due. Throws if they don't on the last try.</summary>
      internal bool KeptWithinTheLimits(StopwatchCE.TimedExecutionSummary executionSummary)
      {
         _tries++;
         if(HowTheTimingsExceededTheLimits(executionSummary) is {} howTheyExceededThem)
         {
            if(_tries >= _maxTries)
               throw new TimingsExceededTheLimitsException($"""
                                                            {_description}:
                                                            {howTheyExceededThem.Indent()}
                                                            """);
            Log.Warning($"Try: {_tries} {howTheyExceededThem}");
            return false;
         }

         PrintSummary(executionSummary);
         Log.Info("""
                  ############################## DONE

                  """);
         return true;
      }

      ///<summary>How long to wait before the next try, after a try that exceeded the limits: exponentially longer each time, starting with 10<br/>
      /// milliseconds, up to half a second.</summary>
      internal TimeSpan PauseBeforeTheNextTry()
      {
         var pause = Math.Min(Math.Pow(2, _tries), 50) * 10.Milliseconds();
         Log.Warning($"Waiting {pause.FormatReadable()} before try {_tries + 1}");
         return pause;
      }

      ///<summary>Null if <paramref name="executionSummary"/> keeps within the limits.</summary>
      string? HowTheTimingsExceededTheLimits(StopwatchCE.TimedExecutionSummary executionSummary)
      {
         if(_maxTotal.HasValue && executionSummary.Total > _maxTotal.Value)
            return $"Total:{executionSummary.Total.FormatReadable()} {Percent(executionSummary.Total, _maxTotal.Value)} of maxTotal: {_maxTotal.FormatReadable()}";

         if(_maxAverage.HasValue && executionSummary.Average > _maxAverage.Value)
            return $" {Percent(executionSummary.Average, _maxAverage.Value)} of maxAverage: {_maxAverage.FormatReadable()}";

         return null;
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

      class TimingsExceededTheLimitsException(string message) : Exception(message);
   }
}
