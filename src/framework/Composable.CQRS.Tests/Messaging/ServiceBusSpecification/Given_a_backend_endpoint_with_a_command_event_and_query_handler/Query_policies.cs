using System;
using System.Threading.Tasks;
using Composable.Messaging.Buses;
using Composable.Testing.Threading;
using NUnit.Framework;

namespace Composable.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;

public class Query_policies : Fixture
{
   [Test] public async Task The_same_query_can_be_reused_in_parallel_without_issues()
   {
      var myQuery = new MyQuery();

      QueryHandlerThreadGate.Close();

      var something = ClientEndpoint.ExecuteClientRequestAsync(async navigator => (await navigator.GetAsync(myQuery), await navigator.GetAsync(myQuery)));

      Console.WriteLine("aoeusnth");
      QueryHandlerThreadGate.AwaitQueueLengthEqualTo(length: 2);
      QueryHandlerThreadGate.Open();

      var (result1, result2) = await something;
   }

   public Query_policies(string _) : base(_) {}
}