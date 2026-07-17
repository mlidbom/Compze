using Compze.Tests.Common.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure.XUnit;

namespace Compze.Tests.Integration.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

public class Async_behavior_test : EndpointHostTestBase
{
   [PCT] public async Task Tuery_returns_task_immediately_does_not_block_until_awaited()
   {
      TueryHandlerThreadGate.Close();

      var tuery = Navigator.GetAsync(new MyTuery());
      TueryHandlerThreadGate.Open();
      await tuery;
   }
}
