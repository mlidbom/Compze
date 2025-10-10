using System.Linq;
using System.Threading.Tasks;
using Compze.Tessaging.Hosting;
using Compze.Utilities.Threading.Testing;
using Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;
using static Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler.Fixture;
using Compze.Tests.Common.NUnit.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;
using Compze.Utilities.SystemCE.LinqCE;
using NUnit.Framework;

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;

public class Query_policies(string pluggableComponentsCombination) : NUnitFixtureBase(pluggableComponentsCombination)
{
   [Test] public async Task The_same_query_can_be_reused_in_parallel_without_issues()
   {
      var myQuery = new MyQuery();

      QueryHandlerThreadGate.Close();

      var queriesResults = Task.WhenAll(1.Through(5)
                                         .Select(_ => ClientEndpoint.ExecuteClientRequestAsync(navigator => navigator.GetAsync(myQuery))));

      QueryHandlerThreadGate.AwaitQueueLengthEqualTo(length: 5);
      QueryHandlerThreadGate.Open();

      await queriesResults;
   }
}
