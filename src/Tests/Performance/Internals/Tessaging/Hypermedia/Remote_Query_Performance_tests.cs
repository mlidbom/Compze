using System;
using System.Linq;
using System.Threading.Tasks;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Typermedia.Public;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Performance;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Tests.Infrastructure.XUnit;
using CreatesItsOwnResultQuery = Compze.Abstractions.Tessaging.Public.MessageTypes.Remotable.NonTransactional.Queries.NewableResultLink<Compze.Tests.Performance.Internals.Tessaging.Hypermedia.PerformanceTestBase.MyQueryResult>;

namespace Compze.Tests.Performance.Internals.Tessaging.Hypermedia;

public class RemoteQueryPerformanceTests : PerformanceTestBase
{
   [PCT]  public void SingleThreaded_Runs_30_local_requests_making_one_remote_query_each_in_50_milliseconds() =>
      RunScenario(threaded: false, requests: 30, queriesPerRequest: 1, maxTotal: 50.Milliseconds().EnvMultiply(instrumented:1.5), query: new MyRemoteQuery());

   [PCT]  public void SingleThreaded_Runs_100_local_requests_making_one_ICreateMyOwnResult_query_each_in_2_milliseconds() =>
      RunScenario(threaded: false, requests: 100, queriesPerRequest: 1, maxTotal: 2.Milliseconds().EnvMultiply(instrumented:2.4), query: new CreatesItsOwnResultQuery());

   [PCT]  public void SingleThreaded_Runs_10_local_requests_making_10_remote_queries_each_in_100_milliseconds() =>
      RunScenario(threaded: false, requests: 10, queriesPerRequest: 10, maxTotal: 100.Milliseconds().EnvMultiply(1.3), query: new MyRemoteQuery());

   [PCT]  public void SingleThreaded_Runs_10_local_requests_making_10_ICreateMyOwnResult_query_each_in_2_milliseconds() =>
      RunScenario(threaded: false, requests: 10, queriesPerRequest: 10, maxTotal: 2.Milliseconds().EnvMultiply(instrumented:1.3), query: new CreatesItsOwnResultQuery());

   [PCT]  public void SingleThreaded_Runs_1_local_request_making_200_ICreateMyOwnResult_query_each_in_1_milliseconds() =>
      RunScenario(threaded: false, requests: 1, queriesPerRequest: 200, maxTotal: 1.Milliseconds().EnvMultiply(instrumented:2.6), query: new CreatesItsOwnResultQuery());

   [PCT]  public void MultiThreaded_Runs_70_local_requests_making_one_remote_query_each_in_20_milliseconds() =>
      RunScenario(threaded: true, requests: 70, queriesPerRequest: 1, maxTotal: 20.Milliseconds().EnvMultiply(instrumented:2.5), query: new MyRemoteQuery());

   [PCT]  public void MultiThreaded_Runs_10_local_requests_making_10_remote_queries_each_in_30_milliseconds() =>
      RunScenario(threaded: true, requests: 10, queriesPerRequest: 10, maxTotal: 30.Milliseconds().EnvMultiply(instrumented:2.5), query: new MyRemoteQuery());

   [PCT]  public void MultiThreaded_Runs_10_local_request_making_200_ICreateMyOwnResult_query_each_in_15_milliseconds() =>
      RunScenario(threaded: true, requests: 10, queriesPerRequest: 200, maxTotal: 15.Milliseconds().EnvMultiply(instrumented:2.2), query: new CreatesItsOwnResultQuery());

   [PCT]  public async Task Async_Runs_100_local_requests_making_one_async_remote_query_each_in_20_milliseconds() =>
      await RunAsyncScenario(requests: 100, queriesPerRequest: 1, maxTotal: 20.Milliseconds().EnvMultiply(instrumented:2.4, unoptimized:1.5), query: new MyRemoteQuery());

   [PCT]  public async Task Async_Runs_10_local_requests_making_10_async_remote_queries_each_in_20_milliseconds() =>
      await RunAsyncScenario(requests: 10, queriesPerRequest: 10, maxTotal: 20.Milliseconds().EnvMultiply(instrumented:3, unoptimized:2.0), query: new MyRemoteQuery());

   [PCT]  public async Task Async_Runs_10_local_request_making_200_ICreateMyOwnResult_query_each_in_15_milliseconds() =>
      await RunAsyncScenario(requests: 10, queriesPerRequest: 200, maxTotal: 15.Milliseconds().EnvMultiply(instrumented:3.4), query: new CreatesItsOwnResultQuery());


   void RunScenario(bool threaded, int requests, int queriesPerRequest, TimeSpan maxTotal, IRemotableQuery<MyQueryResult> query)
   {
      var navigationSpecification = NavigationSpecification.Get(query);

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
      void RunRequest() =>
         ClientEndpoint.ServiceLocator.ExecuteInIsolatedScope(() =>
         {
            for(var i = 0; i < queriesPerRequest; i++)
            {
               RemoteNavigator.Navigate(navigationSpecification);
            }
         });
   }

   async Task RunAsyncScenario(int requests, int queriesPerRequest, TimeSpan maxTotal, IRemotableQuery<MyQueryResult> query)
   {
      var navigationSpecification = NavigationSpecification.Get(query);

      //ncrunch: no coverage end

      //Warmup
      await RunScenarioAsync();

      await TimeAsserter.ExecuteAsync(RunScenarioAsync, maxTotal: maxTotal);
      return;

      //ncrunch: no coverage start
      async Task RunRequestAsync() =>
         await ClientEndpoint.ServiceLocator.ExecuteInIsolatedScopeAsync(
            async () => await Task.WhenAll(1.Through(queriesPerRequest)
                                            .Select(_ => RemoteNavigator.NavigateAsync(navigationSpecification))
                                            .ToArray()));

      async Task RunScenarioAsync() => await Task.WhenAll(1.Through(requests).Select(_ => RunRequestAsync()).ToArray());
   }
}