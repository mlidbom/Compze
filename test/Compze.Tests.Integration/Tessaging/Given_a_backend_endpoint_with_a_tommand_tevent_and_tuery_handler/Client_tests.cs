using Compze.Tests.Common.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Must;

using Compze.Tessaging.Typermedia;

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Integration.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

public class Client_tests : EndpointHostTestBase
{
   [PCT] public void Can_execute_tuery_via_Navigator()
   {
      var result = Navigator.Get(new MyTuery());
      result.Must().NotBeNull();
   }

   [PCT] public void Can_post_tommand_via_Navigator()
   {
      var result = Navigator.Post(MyAtMostOnceTypermediaTommandWithResult.Create());
      result.Must().NotBeNull();
   }

   [PCT] public void Can_execute_tuery_via_Navigator_using_NavigationSpecification()
   {
      var result = Navigator.Navigate(NavigationSpecification.Get(new MyTuery()));
      result.Must().NotBeNull();
   }
}
