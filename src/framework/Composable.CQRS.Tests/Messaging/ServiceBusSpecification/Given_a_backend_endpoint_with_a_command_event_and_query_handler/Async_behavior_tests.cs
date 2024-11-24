using System.Threading.Tasks;
using Composable.Messaging;
using Composable.Messaging.Hypermedia;
using Composable.SystemCE.ThreadingCE.TasksCE;
using NUnit.Framework;

namespace Composable.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;

public class Async_behavior_test : Fixture
{
   public Async_behavior_test(string _) : base(_) {}

   [Test] public async Task Query_returns_task_immediately_does_not_block_until_awaited()
   {
      QueryHandlerThreadGate.Close();

      using var _ = ClientEndpoint.ServiceLocator.BeginScope();
      var session = ClientEndpoint.ServiceLocator.Resolve<IRemoteHypermediaNavigator>();
      var query = session.GetAsync(new MyQuery());
      // QueryHandlerThreadGate.Open();
      // await query;
      await Task.CompletedTask;
   }
   
   [Test] public async Task Fakes_query_returns_task_immediately_does_not_block_until_awaited()
   {
      QueryHandlerThreadGate.Close();

      using var _ = ClientEndpoint.ServiceLocator.BeginScope();
      var session = new RemoteHypermediaNavigatorFake();
      var query = session.GetAsync(new MyQuery());
      // QueryHandlerThreadGate.Open();
      // await query;
      await Task.CompletedTask;
   }

   class RemoteHypermediaNavigatorFake
   {
      readonly TransportFake _transport = new();
      public async Task<TResult> GetAsync<TResult>(IRemotableQuery<TResult> query) { return await GetAsyncAfterFastPathOptimization(query).NoMarshalling(); }

      async Task<TResult> GetAsyncAfterFastPathOptimization<TResult>(IRemotableQuery<TResult> query) => await _transport.GetAsync(query).NoMarshalling();
   }

   class TransportFake
   {
      readonly OutBoxConnectionFake _connection = new();
      public async Task<TQueryResult> GetAsync<TQueryResult>(IRemotableQuery<TQueryResult> query) { return await _connection.GetAsync(query).NoMarshalling(); }
   }

   class OutBoxConnectionFake
   {
      public async Task<TQueryResult> GetAsync<TQueryResult>(IRemotableQuery<TQueryResult> query)
      {
         return await new TaskCompletionSource<TQueryResult>().Task.NoMarshalling();
      }
   }
}
