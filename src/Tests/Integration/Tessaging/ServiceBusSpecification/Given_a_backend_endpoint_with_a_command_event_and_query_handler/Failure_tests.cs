using System;
using System.Threading.Tasks;
using Compze.Tessaging.Hosting;
using Compze.Utilities.Threading.Testing;
using Compze.Tests.Common.NUnit.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;
using Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;
using FluentAssertions;
using NUnit.Framework;

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;

public class Failure_tests(string pluggableComponentsCombination) : NUnitFixtureBase(pluggableComponentsCombination)
{
   [Test] public async Task If_command_handler_with_result_throws_awaiting_SendAsync_throws()
   {
      CommandHandlerWithResultThreadGate.ThrowPostPassThrough(_thrownException);
      await FluentActions.Invoking(async () => await ClientEndpoint.ExecuteClientRequestAsync(async session => await session.PostAsync(MyAtMostOnceCommandWithResult.Create())))
                   .Should().ThrowAsync<Exception>();
   }

   [Test] public async Task If_query_handler_throws_awaiting_QueryAsync_throws()
   {
      QueryHandlerThreadGate.ThrowPostPassThrough(_thrownException);
      await FluentActions.Invoking(() => ClientEndpoint.ExecuteClientRequestAsync(session => session.GetAsync(new MyQuery())))
                         .Should().ThrowAsync<Exception>();
   }

   [Test] public void If_query_handler_throws_Query_throws()
   {
      QueryHandlerThreadGate.ThrowPostPassThrough(_thrownException);
      var exception = FluentActions.Invoking(() => ClientEndpoint.ExecuteClientRequest(session => session.Get(new MyQuery()))).Should().Throw<Exception>().Which;
      exception.Should().NotBeNull();
   }

   public override async Task TearDownAsync()
   {
      var exception = await FluentActions.Invoking(async () => await Host.DisposeAsync()).Should().ThrowAsync<Exception>();
      exception.Which.Should().NotBeNull();
      await base.TearDownAsync();
   }

   readonly IntentionalException _thrownException = new();
   class IntentionalException : Exception;
}