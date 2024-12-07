using System;
using System.Threading.Tasks;
using Compze.Messaging.Buses;
using Compze.Testing.Threading;
using Compze.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;
using FluentAssertions;
using NUnit.Framework;
using Assert = Xunit.Assert;

namespace Compze.Tests.Integration.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;

public class Failure_tests(string pluggableComponentsCombination) : Fixture(pluggableComponentsCombination)
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
      Assert.ThrowsAny<Exception>(() => ClientEndpoint.ExecuteClientRequest(session => session.Get(new MyQuery())));
   }

   public override async Task TearDownAsync()
   {
      await Assert.ThrowsAnyAsync<Exception>(async Task() => await Host.DisposeAsync());
      await base.TearDownAsync();
   }

   readonly IntentionalException _thrownException = new();
   class IntentionalException : Exception;
}