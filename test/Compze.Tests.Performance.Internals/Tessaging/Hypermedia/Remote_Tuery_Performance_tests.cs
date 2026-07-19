using Compze.Internals.Testing;
using Compze.Internals.Testing.Performance;
using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Tessaging.Abstractions.Public;
using Compze.Tessaging.Abstractions.TessageTypes;
using Compze.Tests.Infrastructure.XUnit;
using CreatesItsOwnResultTuery = Compze.Tessaging.Abstractions.TessageTypes.TessageTypes.Remotable.NonTransactional.Tueries.NewableResultLink<Compze.Tests.Performance.Internals.Tessaging.Hypermedia.PerformanceTestBase.MyTueryResult>;
using Compze.Tessaging.Typermedia;

namespace Compze.Tests.Performance.Internals.Tessaging.Hypermedia;

public class RemoteTueryPerformanceTests : PerformanceTestBase
{
   [PCT]  public void SingleThreaded_Runs_30_local_requests_making_one_remote_tuery_each_in_50_milliseconds() =>
      RunScenario(threaded: false, requests: 30, tueriesPerRequest: 1, maxTotal: 50.Milliseconds().EnvMultiply(instrumented:1.5), tuery: new MyRemoteTuery());

   [PCT]  public void SingleThreaded_Runs_100_local_requests_making_one_ICreateMyOwnResult_tuery_each_in_2_milliseconds() =>
      RunScenario(threaded: false, requests: 100, tueriesPerRequest: 1, maxTotal: 2.Milliseconds().EnvMultiply(instrumented:2.4), tuery: new CreatesItsOwnResultTuery());

   [PCT]  public void SingleThreaded_Runs_10_local_requests_making_10_remote_tueries_each_in_100_milliseconds() =>
      RunScenario(threaded: false, requests: 10, tueriesPerRequest: 10, maxTotal: 100.Milliseconds().EnvMultiply(1.3), tuery: new MyRemoteTuery());

   [PCT]  public void SingleThreaded_Runs_10_local_requests_making_10_ICreateMyOwnResult_tuery_each_in_2_milliseconds() =>
      RunScenario(threaded: false, requests: 10, tueriesPerRequest: 10, maxTotal: 2.Milliseconds().EnvMultiply(instrumented:1.3), tuery: new CreatesItsOwnResultTuery());

   [PCT]  public void SingleThreaded_Runs_1_local_request_making_200_ICreateMyOwnResult_tuery_each_in_1_milliseconds() =>
      RunScenario(threaded: false, requests: 1, tueriesPerRequest: 200, maxTotal: 1.Milliseconds().EnvMultiply(instrumented:2.6), tuery: new CreatesItsOwnResultTuery());

   [PCT]  public void MultiThreaded_Runs_70_local_requests_making_one_remote_tuery_each_in_20_milliseconds() =>
      RunScenario(threaded: true, requests: 70, tueriesPerRequest: 1, maxTotal: 20.Milliseconds().EnvMultiply(instrumented:2.5), tuery: new MyRemoteTuery());

   [PCT]  public void MultiThreaded_Runs_10_local_requests_making_10_remote_tueries_each_in_30_milliseconds() =>
      RunScenario(threaded: true, requests: 10, tueriesPerRequest: 10, maxTotal: 30.Milliseconds().EnvMultiply(instrumented:2.5), tuery: new MyRemoteTuery());

   [PCT]  public void MultiThreaded_Runs_10_local_request_making_200_ICreateMyOwnResult_tuery_each_in_15_milliseconds() =>
      RunScenario(threaded: true, requests: 10, tueriesPerRequest: 200, maxTotal: 15.Milliseconds().EnvMultiply(instrumented:2.2), tuery: new CreatesItsOwnResultTuery());

   [PCT]  public async Task Async_Runs_100_local_requests_making_one_async_remote_tuery_each_in_20_milliseconds() =>
      await RunAsyncScenario(requests: 100, tueriesPerRequest: 1, maxTotal: 20.Milliseconds().EnvMultiply(instrumented:2.4, unoptimized:1.5), tuery: new MyRemoteTuery());

   [PCT]  public async Task Async_Runs_10_local_requests_making_10_async_remote_tueries_each_in_20_milliseconds() =>
      await RunAsyncScenario(requests: 10, tueriesPerRequest: 10, maxTotal: 20.Milliseconds().EnvMultiply(instrumented:3, unoptimized:2.0), tuery: new MyRemoteTuery());

   [PCT]  public async Task Async_Runs_10_local_request_making_200_ICreateMyOwnResult_tuery_each_in_15_milliseconds() =>
      await RunAsyncScenario(requests: 10, tueriesPerRequest: 200, maxTotal: 15.Milliseconds().EnvMultiply(instrumented:3.4), tuery: new CreatesItsOwnResultTuery());


   void RunScenario(bool threaded, int requests, int tueriesPerRequest, TimeSpan maxTotal, IRemotableTuery<MyTueryResult> tuery)
   {
      var navigationSpecification = NavigationSpecification.Get(tuery);

      //ncrunch: no coverage end

      if(threaded)
      {
         StopwatchCE.TimeExecutionThreaded(RunRequest, iterations: requests); //Warmup
         TimeAsserter.ExecuteThreaded(RunRequest, iterations: requests, maxTotal: maxTotal);
      } else
      {
         StopwatchCE.TimeExecution(RunRequest, iterations: requests); //Warmup
         TimeAsserter.Execute(RunRequest, iterations: requests, maxTotal: maxTotal);
      }

      return;

      //ncrunch: no coverage start
      void RunRequest()
      {
         for(var i = 0; i < tueriesPerRequest; i++)
         {
            Navigator.Navigate(navigationSpecification);
         }
      }
   }

   async Task RunAsyncScenario(int requests, int tueriesPerRequest, TimeSpan maxTotal, IRemotableTuery<MyTueryResult> tuery)
   {
      var navigationSpecification = NavigationSpecification.Get(tuery);

      //ncrunch: no coverage end

      //Warmup
      await RunScenarioAsync();

      await TimeAsserter.ExecuteAsync(RunScenarioAsync, maxTotal: maxTotal);
      return;

      //ncrunch: no coverage start
      async Task RunRequestAsync() =>
         await Task.WhenAll(1.Through(tueriesPerRequest)
                                            .Select(_ => Navigator.NavigateAsync(navigationSpecification))
                                            .ToArray());

      async Task RunScenarioAsync() => await Task.WhenAll(1.Through(requests).Select(_ => RunRequestAsync()).ToArray());
   }
}