using System;
using System.Threading.Tasks;
using Compze.Tessaging.Hosting;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Tests.Common.NUnit.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;
using Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;
using Compze.Utilities.Threading.Testing;
using FluentAssertions;
using NUnit.Framework;

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;

public class Fixture_tests(string pluggableComponentsCombination) : NUnitFixtureBase(pluggableComponentsCombination)
{
   [Test] public async Task If_command_handler_throws_disposing_host_throws_AggregateException_containing_the_thrown_exception()
   {
      MyExactlyOnceCommandHandlerThreadGate.ThrowPostPassThrough(_thrownException);
      RemoteEndpoint.ExecuteServerRequestInTransaction(session => session.Send(new MyExactlyOnceCommand()));
      await AssertDisposingHostThrowsAggregateExceptionHierarchyContainingThrownExceptionAsANonAggregateException();
   }

   [Test] public async Task If_command_handler_with_result_throws_disposing_host_throws_AggregateException_containing_the_thrown_exception_and_SendAsync_throws_MessageDispatchingFailedException()
   {
      CommandHandlerWithResultThreadGate.ThrowPostPassThrough(_thrownException);
      await FluentActions.Invoking(async () => await ClientEndpoint.ExecuteClientRequest(async session => await session.PostAsync(MyAtMostOnceCommandWithResult.Create())))
                         .Should().ThrowAsync<MessageDispatchingFailedException>();

      await AssertDisposingHostThrowsAggregateExceptionHierarchyContainingThrownExceptionAsANonAggregateException();
   }

   [Test] public async Task If_event_handler_throws_disposing_host_throws_AggregateException_containing_the_thrown_exception()
   {
      MyRemoteAggregateEventHandlerThreadGate.ThrowPostPassThrough(_thrownException);
      ClientEndpoint.ExecuteClientRequest(session => session.Post(MyCreateAggregateCommand.Create()));
      await AssertDisposingHostThrowsAggregateExceptionHierarchyContainingThrownExceptionAsANonAggregateException();
   }

   [Test] public async Task If_query_handler_throws_disposing_host_throws_AggregateException_containing_the_thrown_exception_and_SendAsync_throws_MessageDispatchingFailedException()
   {
      QueryHandlerThreadGate.ThrowPostPassThrough(_thrownException);
      await FluentActions.Invoking(() => ClientEndpoint.ExecuteClientRequest(session => session.GetAsync(new MyQuery())))
                         .Should().ThrowAsync<MessageDispatchingFailedException>();

      await AssertDisposingHostThrowsAggregateExceptionHierarchyContainingThrownExceptionAsANonAggregateException();
   }

   async Task AssertDisposingHostThrowsAggregateExceptionHierarchyContainingThrownExceptionAsANonAggregateException()
   {
      var exception = (await FluentActions.Invoking(async Task () => await Host.DisposeAsync())
                                          .Should().ThrowAsync<AggregateException>()).Which;
      var rootExceptions = exception.Flatten().InnerExceptions;
      rootExceptions.Should().Contain(_thrownException);
   }

   readonly IntentionalException _thrownException = new();
   class IntentionalException : Exception;
}
