using System;
using System.Threading.Tasks;
using Compze.Tessaging.Hosting;
using Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Utilities.SystemCE.ThreadingCE.Testing;
using Compze.Utilities.Testing.Fluent;
using static Compze.Utilities.Testing.Fluent.MustActions;
namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

public class Failure_tests : EndpointHostTestBase
{
   [PCT] public async Task If_tommand_handler_with_result_throws_awaiting_SendAsync_throws()
   {
      TommandHandlerWithResultThreadGate.ThrowPostPassThrough(_thrownException);
      await InvokingAsync(async () => await ClientEndpoint.ExecuteClientRequestAsync(async session => await session.PostAsync(MyAtMostOnceTypermediaTommandWithResult.Create())))
                   .Must().ThrowAsync<Exception>();
   }

   [PCT] public async Task If_tuery_handler_throws_awaiting_TueryAsync_throws()
   {
      TueryHandlerThreadGate.ThrowPostPassThrough(_thrownException);
      await InvokingAsync(() => ClientEndpoint.ExecuteClientRequestAsync(session => session.GetAsync(new MyTuery())))
           .Must().ThrowAsync<Exception>();
   }

   [PCT] public void If_tuery_handler_throws_Tuery_throws()
   {
      TueryHandlerThreadGate.ThrowPostPassThrough(_thrownException);
      var exception = Invoking(() => ClientEndpoint.ExecuteClientRequest(session => session.Get(new MyTuery()))).Must().Throw<Exception>().Which;
      exception.Must().NotBeNull();
   }

   protected override async Task DisposeAsyncInternal()
   {
      var exception = await InvokingAsync(async () => await Host.DisposeAsync()).Must().ThrowAsync<Exception>();
      exception.Which.Must().NotBeNull();
      await base.DisposeAsyncInternal();
   }

   readonly IntentionalException _thrownException = new();
   class IntentionalException : Exception;
}
