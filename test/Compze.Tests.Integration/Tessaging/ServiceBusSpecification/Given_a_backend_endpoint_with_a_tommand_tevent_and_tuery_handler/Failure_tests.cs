using Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading.Testing;
using Compze.Must;
using static Compze.Must.MustActions;
namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

public class Failure_tests : EndpointHostTestBase
{
   [PCT] public async Task If_tommand_handler_with_result_throws_awaiting_SendAsync_throws()
   {
      TommandHandlerWithResultThreadGate.ThrowPostPassThrough(_thrownException);
      await InvokingAsync(async () => await Client.ExecuteRequestAsync(async session => await session.PostAsync(MyAtMostOnceTypermediaTommandWithResult.Create())))
                   .Must().ThrowAsync<Exception>();
   }

   [PCT] public async Task If_tuery_handler_throws_awaiting_TueryAsync_throws()
   {
      TueryHandlerThreadGate.ThrowPostPassThrough(_thrownException);
      await InvokingAsync(() => Client.ExecuteRequestAsync(session => session.GetAsync(new MyTuery())))
           .Must().ThrowAsync<Exception>();
   }

   [PCT] public void If_tuery_handler_throws_Tuery_throws()
   {
      TueryHandlerThreadGate.ThrowPostPassThrough(_thrownException);
      var exception = Invoking(() => Client.ExecuteRequest(session => session.Get(new MyTuery()))).Must().Throw<Exception>().Which;
      exception.Must().NotBeNull();
   }

   readonly IntentionalException _thrownException = new();
   class IntentionalException : Exception;
}
