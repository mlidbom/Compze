using System.Linq;
using System.Threading.Tasks;
using Compze.Tessaging.Hosting;
using Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading.Testing;

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

public class Tuery_policies : EndpointHostTestBase
{
   [PCT] public async Task The_same_tuery_can_be_reused_in_parallel_without_issues()
   {
      var myTuery = new MyTuery();

      TueryHandlerThreadGate.Close();

      var tueriesResults = Task.WhenAll(1.Through(5)
                                         .Select(_ => Client.ExecuteRequestAsync(navigator => navigator.GetAsync(myTuery))));

      TueryHandlerThreadGate.AwaitQueueLengthEqualTo(length: 5);
      TueryHandlerThreadGate.Open();

      await tueriesResults;
   }
}
