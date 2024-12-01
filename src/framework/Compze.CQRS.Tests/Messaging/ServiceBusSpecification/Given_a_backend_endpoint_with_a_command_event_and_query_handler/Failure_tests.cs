using System;
using System.Threading.Tasks;
using Composable.Messaging.Buses;
using Composable.SystemCE.ThreadingCE.TasksCE;
using Composable.Testing;
using Composable.Testing.Threading;
using NUnit.Framework;
using Assert = Xunit.Assert;

namespace Composable.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;

public class Failure_tests(string pluggableComponentsCombination) : Fixture(pluggableComponentsCombination)
{
   [Test] public async Task If_command_handler_with_result_throws_awaiting_SendAsync_throws()
   {
      CommandHandlerWithResultThreadGate.ThrowPostPassThrough(_thrownException);
      await AssertThrows.Async<Exception>(async () => await ClientEndpoint.ExecuteClientRequestAsync(session => session.PostAsync(MyAtMostOnceCommandWithResult.Create())).CaF()).CaF();
   }

   [Test] public async Task If_query_handler_throws_awaiting_QueryAsync_throws()
   {
      QueryHandlerThreadGate.ThrowPostPassThrough(_thrownException);
      await AssertThrows.Async<Exception>(() => ClientEndpoint.ExecuteClientRequestAsync(session => session.GetAsync(new MyQuery()))).CaF();
   }

   [Test] public void If_query_handler_throws_Query_throws()
   {
      QueryHandlerThreadGate.ThrowPostPassThrough(_thrownException);
      Assert.ThrowsAny<Exception>(() => ClientEndpoint.ExecuteClientRequest(session => session.Get(new MyQuery())));
   }

   public override async Task TearDownAsync()
   {
      await Assert.ThrowsAnyAsync<Exception>(async Task() => await Host.DisposeAsync().CaF()).CaF();
      await base.TearDownAsync().CaF();
   }

   readonly IntentionalException _thrownException = new();
   class IntentionalException : Exception;
}