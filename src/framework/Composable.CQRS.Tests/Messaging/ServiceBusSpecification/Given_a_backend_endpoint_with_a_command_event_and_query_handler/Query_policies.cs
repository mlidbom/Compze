using System.Linq;
using System.Threading.Tasks;
using Composable.Messaging.Buses;
using Composable.SystemCE.LinqCE;
using Composable.SystemCE.ThreadingCE.TasksCE;
using Composable.Testing.Threading;
using NUnit.Framework;

namespace Composable.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;

public class Query_policies : Fixture
{
   [Test] public async Task The_same_query_can_be_reused_in_parallel_without_issues()
   {
      var myQuery = new MyQuery();

      QueryHandlerThreadGate.Close();

      var queriesResults = Task.WhenAll(1.Through(5)
                                         .Select(_ => ClientEndpoint.ExecuteClientRequestAsync(navigator => navigator.GetAsync(myQuery))));

      QueryHandlerThreadGate.AwaitQueueLengthEqualTo(length: 5);
      QueryHandlerThreadGate.Open();

      await queriesResults.NoMarshalling();
   }

   public Query_policies(string _) : base(_) {}
}
