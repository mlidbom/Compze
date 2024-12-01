using System;
using System.Linq;
using System.Threading.Tasks;
using Compze.Messaging.Buses;
using Compze.Messaging.Buses.Implementation;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Compze.Testing;
using Compze.Testing.Threading;
using FluentAssertions;
using NUnit.Framework;

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;

class Fixture_tests(string pluggableComponentsCombination) : Fixture(pluggableComponentsCombination)
{
   [Test] public async Task If_command_handler_throws_disposing_host_throws_AggregateException_containing_a_single_exception_that_is_the_thrown_exception()
   {
      CommandHandlerThreadGate.ThrowPostPassThrough(_thrownException);
      RemoteEndpoint.ExecuteServerRequestInTransaction(session => session.Send(new MyExactlyOnceCommand()));
      await AssertDisposingHostThrowsAggregateExceptionHierarchyContainingOnlyThrownExceptionAsANonAggregateException().CaF();
   }

   [Test] public async Task If_command_handler_with_result_throws_disposing_host_throws_AggregateException_containing_a_single_exception_that_is_the_thrown_exception_and_SendAsync_throws_MessageDispatchingFailedException()
   {
      CommandHandlerWithResultThreadGate.ThrowPostPassThrough(_thrownException);
      await AssertThrows.Async<MessageDispatchingFailedException>(async () => await ClientEndpoint.ExecuteClientRequest(session => session.PostAsync(MyAtMostOnceCommandWithResult.Create())).CaF()).CaF();

      await AssertDisposingHostThrowsAggregateExceptionHierarchyContainingOnlyThrownExceptionAsANonAggregateException().CaF();
   }

   [Test] public async Task If_event_handler_throws_disposing_host_throws_AggregateException_containing_a_single_exception_that_is_the_thrown_exception()
   {
      MyRemoteAggregateEventHandlerThreadGate.ThrowPostPassThrough(_thrownException);
      ClientEndpoint.ExecuteClientRequest(session => session.Post(MyCreateAggregateCommand.Create()));
      await AssertDisposingHostThrowsAggregateExceptionHierarchyContainingOnlyThrownExceptionAsANonAggregateException().CaF();
   }

   [Test] public async Task If_query_handler_throws_disposing_host_throws_AggregateException_containing_a_single_exception_that_is_the_thrown_exception_and_SendAsync_throws_MessageDispatchingFailedException()
   {
      QueryHandlerThreadGate.ThrowPostPassThrough(_thrownException);
      await AssertThrows.Async<MessageDispatchingFailedException>(() => ClientEndpoint.ExecuteClientRequest(session => session.GetAsync(new MyQuery()))).CaF();

      await AssertDisposingHostThrowsAggregateExceptionHierarchyContainingOnlyThrownExceptionAsANonAggregateException().CaF();
   }

   async Task AssertDisposingHostThrowsAggregateExceptionHierarchyContainingOnlyThrownExceptionAsANonAggregateException()
   {
      var exception = await AssertThrows.Async<AggregateException>(async Task () => await Host.DisposeAsync().CaF()).CaF();
      var rootExceptions = exception.Flatten().InnerExceptions;
      rootExceptions.Should().HaveCount(1);
      rootExceptions.Single().Should().Be(_thrownException);
   }

   readonly IntentionalException _thrownException = new();
   class IntentionalException : Exception;
}