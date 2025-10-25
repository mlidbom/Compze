using System;
using System.Threading.Tasks;
using Compze.Tessaging.Hosting;
using Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_tuery_handler;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Utilities.Threading.Testing;
using FluentAssertions;

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_tuery_handler;

public class Failure_tests : EndpointHostTestBase
{
   [PCT] public async Task If_command_handler_with_result_throws_awaiting_SendAsync_throws()
   {
      CommandHandlerWithResultThreadGate.ThrowPostPassThrough(_thrownException);
      await FluentActions.Invoking(async () => await ClientEndpoint.ExecuteClientRequestAsync(async session => await session.PostAsync(MyAtMostOnceTommandWithResult.Create())))
                   .Should().ThrowAsync<Exception>();
   }

   [PCT] public async Task If_tuery_handler_throws_awaiting_TueryAsync_throws()
   {
      TueryHandlerThreadGate.ThrowPostPassThrough(_thrownException);
      await FluentActions.Invoking(() => ClientEndpoint.ExecuteClientRequestAsync(session => session.GetAsync(new MyTuery())))
                         .Should().ThrowAsync<Exception>();
   }

   [PCT] public void If_tuery_handler_throws_Tuery_throws()
   {
      TueryHandlerThreadGate.ThrowPostPassThrough(_thrownException);
      var exception = FluentActions.Invoking(() => ClientEndpoint.ExecuteClientRequest(session => session.Get(new MyTuery()))).Should().Throw<Exception>().Which;
      exception.Should().NotBeNull();
   }

   protected override async Task DisposeAsyncInternal()
   {
      var exception = await FluentActions.Invoking(async () => await Host.DisposeAsync()).Should().ThrowAsync<Exception>();
      exception.Which.Should().NotBeNull();
      await base.DisposeAsyncInternal();
   }

   readonly IntentionalException _thrownException = new();
   class IntentionalException : Exception;
}