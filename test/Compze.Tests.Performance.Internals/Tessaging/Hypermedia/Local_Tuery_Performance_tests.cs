using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Performance;
using Compze.DependencyInjection;
using Compze.Utilities.SystemCE;
using Compze.Tests.Infrastructure.XUnit;

namespace Compze.Tests.Performance.Internals.Tessaging.Hypermedia;

public class Local_Tuery_performance_tests : PerformanceTestBase
{
   [PCT]  public void Runs_10_000__MultiThreaded_local_requests_making_a_single_local_tuery_each_in_30_milliseconds() =>
      RunScenario(threaded: true, requests: 10_000.EnvDivide(instrumented:12), tueriesPerRequest: 1, maxTotal: 30.Milliseconds());

   [PCT]  public void Runs_10_000_SingleThreaded_local_requests_making_a_single_local_tuery_each_in_80_milliseconds() =>
      RunScenario(threaded: false, requests: 10_000.EnvDivide(instrumented:6), tueriesPerRequest: 1, maxTotal: 80.Milliseconds());

   [PCT]  public void Runs_4_000__MultiThreaded_local_requests_making_10_local_tueries_each_in_50_milliseconds() =>
      RunScenario(threaded: true, requests: 4_000.EnvDivide(instrumented:12), tueriesPerRequest: 10, maxTotal: 50.Milliseconds());

   [PCT]  public void Runs_10_000__SingleThreaded_local_requests_making_10_local_tueries_each_in_170_milliseconds() =>
      RunScenario(threaded: false, requests: 10_000.EnvDivide(instrumented:6), tueriesPerRequest: 10, maxTotal: 170.Milliseconds());

   void RunScenario(bool threaded, int requests, int tueriesPerRequest, TimeSpan maxTotal)
   {
      //ncrunch: no coverage end

      if(threaded)
      {
         StopwatchCE.TimeExecutionThreaded(RunRequest, iterations: requests);
         TimeAsserter.ExecuteThreaded(RunRequest, iterations: requests, maxTotal: maxTotal);
      } else
      {
         StopwatchCE.TimeExecution(RunRequest, iterations: requests);
         TimeAsserter.Execute(RunRequest, iterations: requests, maxTotal: maxTotal);
      }

      return;

      //ncrunch: no coverage start
      void RunRequest() =>
         ServerEndpoint.ServiceLocator.ExecuteInIsolatedScope(() =>
         {
            for(var i = 0; i < tueriesPerRequest; i++)
            {
               InProcessNavigator.Execute(new MyLocalStrictlyLocalTuery());
            }
         });
   }
}