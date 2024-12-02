using System;
using System.Linq;
using System.Threading.Tasks;
using Compze.DependencyInjection;
using Compze.Messaging;
using Compze.Messaging.Hypermedia;
using Compze.SystemCE;
using Compze.SystemCE.DiagnosticsCE;
using Compze.SystemCE.LinqCE;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Compze.Testing;
using Compze.Testing.Performance;
using NUnit.Framework;
using CreatesItsOwnResultQuery = Compze.Messaging.MessageTypes.Remotable.NonTransactional.Queries.NewableResultLink<Compze.Tests.Messaging.Hypermedia.PerformanceTestBase.MyQueryResult>;

namespace Compze.Tests.Messaging.Hypermedia;

public class RemoteQueryPerformanceTests(string pluggableComponentsCombination) : PerformanceTestBase(pluggableComponentsCombination)
{
   [Test] public void SingleThreaded_Runs_100_local_requests_making_one_remote_query_each_in_60_milliseconds() =>
      RunScenario(threaded: false, requests: 100, queriesPerRequest: 1, maxTotal: 60.Milliseconds().EnvMultiply(instrumented:1.5), query: new MyRemoteQuery());

   [Test] public void SingleThreaded_Runs_100_local_requests_making_one_ICreateMyOwnResult_query_each_in_2_milliseconds() =>
      RunScenario(threaded: false, requests: 100, queriesPerRequest: 1, maxTotal: 2.Milliseconds().EnvMultiply(instrumented:2.4), query: new CreatesItsOwnResultQuery());

   [Test] public void SingleThreaded_Runs_10_local_requests_making_10_remote_queries_each_in_60_milliseconds() =>
      RunScenario(threaded: false, requests: 10, queriesPerRequest: 10, maxTotal: 60.Milliseconds().EnvMultiply(1.3), query: new MyRemoteQuery());

   [Test] public void SingleThreaded_Runs_10_local_requests_making_10_ICreateMyOwnResult_query_each_in_1_milliseconds() =>
      RunScenario(threaded: false, requests: 10, queriesPerRequest: 10, maxTotal: 1.Milliseconds().EnvMultiply(instrumented:1.3), query: new CreatesItsOwnResultQuery());

   [Test] public void SingleThreaded_Runs_1_local_request_making_200_ICreateMyOwnResult_query_each_in_1_milliseconds() =>
      RunScenario(threaded: false, requests: 1, queriesPerRequest: 200, maxTotal: 1.Milliseconds().EnvMultiply(instrumented:2.6), query: new CreatesItsOwnResultQuery());

   [Test] public void MultiThreaded_Runs_100_local_requests_making_one_remote_query_each_in_12_milliseconds() =>
      RunScenario(threaded: true, requests: 100, queriesPerRequest: 1, maxTotal: 12.Milliseconds().EnvMultiply(instrumented:2.5), query: new MyRemoteQuery());

   [Test] public void MultiThreaded_Runs_10_local_requests_making_10_remote_queries_each_in_15_milliseconds() =>
      RunScenario(threaded: true, requests: 10, queriesPerRequest: 10, maxTotal: 15.Milliseconds().EnvMultiply(instrumented:2.5), query: new MyRemoteQuery());

   [Test] public void MultiThreaded_Runs_10_local_request_making_200_ICreateMyOwnResult_query_each_in_3_milliseconds() =>
      RunScenario(threaded: true, requests: 10, queriesPerRequest: 200, maxTotal: 3.Milliseconds().EnvMultiply(instrumented:2.2), query: new CreatesItsOwnResultQuery());

   [Test] public async Task Async_Runs_100_local_requests_making_one_async_remote_query_each_in_10_milliseconds() =>
      await RunAsyncScenario(requests: 100, queriesPerRequest: 1, maxTotal: 10.Milliseconds().EnvMultiply(instrumented:2.4, unoptimized:1.5), query: new MyRemoteQuery()).CaF();

   [Test] public async Task Async_Runs_10_local_requests_making_10_async_remote_queries_each_in_7_milliseconds() =>
      await RunAsyncScenario(requests: 10, queriesPerRequest: 10, maxTotal: 7.Milliseconds().EnvMultiply(instrumented:3, unoptimized:2.0), query: new MyRemoteQuery()).CaF();

   [Test] public async Task Async_Runs_10_local_request_making_200_ICreateMyOwnResult_query_each_in_9_milliseconds() =>
      await RunAsyncScenario(requests: 10, queriesPerRequest: 200, maxTotal: 9.Milliseconds().EnvMultiply(instrumented:3.4), query: new CreatesItsOwnResultQuery()).CaF();


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
      await RunScenarioAsync().CaF();

      await TimeAsserter.ExecuteAsync(RunScenarioAsync, maxTotal: maxTotal).CaF();
      return;

      //ncrunch: no coverage start
      async Task RunRequestAsync() =>
         await ClientEndpoint.ServiceLocator.ExecuteInIsolatedScopeAsync(
            async () => await Task.WhenAll(1.Through(queriesPerRequest)
                                            .Select(_ => RemoteNavigator.NavigateAsync(navigationSpecification))
                                            .ToArray()).CaF()).CaF();

      async Task RunScenarioAsync() => await Task.WhenAll(1.Through(requests).Select(_ => RunRequestAsync()).ToArray()).CaF();
   }
}