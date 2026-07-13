using Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure.XUnit;

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

public class Publisher_conscious_subscription_tests : EndpointHostTestBase
{
   [PCT] public void Remote_subscriber_to_the_taggregates_wrapper_type_receives_the_wrapped_tevent_the_taggregate_published()
   {
      Navigator.Post(MyCreateTaggregateTommand.Create());

      MyRemotePublisherConsciousTeventHandlerThreadGate.AwaitPassedThroughCountEqualTo(1);
   }
}
