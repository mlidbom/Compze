using Compze.Tessaging.Hosting;
using Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Utilities.Testing.Must;

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

public class Client_tests : EndpointHostTestBase
{
   [PCT] public void Can_execute_tuery_via_IClient()
   {
      var result = Client.ExecuteRequest(navigator => navigator.Get(new MyTuery()));
      result.Must().NotBeNull();
   }

   [PCT] public void Can_post_tommand_via_IClient()
   {
      var result = Client.ExecuteRequest(navigator => navigator.Post(MyAtMostOnceTypermediaTommandWithResult.Create()));
      result.Must().NotBeNull();
   }

   [PCT] public void Can_execute_tuery_via_IClient_using_NavigationSpecification()
   {
      var result = Client.ExecuteRequest(Core.Tessaging.Typermedia.Public.NavigationSpecification.Get(new MyTuery()));
      result.Must().NotBeNull();
   }
}
