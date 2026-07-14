using Compze.Tessaging.Hosting;
using Compze.Internals.Transport;
using Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Must;
using Compze.Must.Assertions;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

public class EndpointHostTest_Tests : EndpointHostTestBase
{
   [PCT]  public async Task If_tommand_handler_throws_disposing_host_throws_AggregateException_containing_the_thrown_exception()
   {
      MyExactlyOnceTommandHandlerThreadGate.ThrowPostPassThrough(_thrownException);
      RemoteEndpoint.ExecuteServerRequestInTransaction(session => session.Send(new MyExactlyOnceTommand()));
      await AssertDisposingHostThrowsAggregateExceptionHierarchyContainingThrownExceptionAsANonAggregateException();
   }

   [PCT]  public async Task If_tommand_handler_with_result_throws_disposing_host_throws_AggregateException_containing_the_thrown_exception_and_SendAsync_throws_MessageDispatchingFailedException()
   {
      TommandHandlerWithResultThreadGate.ThrowPostPassThrough(_thrownException);
      await InvokingAsync(async () => await Navigator.PostAsync(MyAtMostOnceTypermediaTommandWithResult.Create()))
                         .Must().ThrowAsync<MessageDispatchingFailedException>();
   }

   [PCT]  public async Task If_tevent_handler_throws_disposing_host_throws_AggregateException_containing_the_thrown_exception()
   {
      MyRemoteTaggregateTeventHandlerThreadGate.ThrowPostPassThrough(_thrownException);
      await Navigator.PostAsync(MyCreateTaggregateTommand.Create());
      await AssertDisposingHostThrowsAggregateExceptionHierarchyContainingThrownExceptionAsANonAggregateException();
   }

   [PCT]  public async Task If_tuery_handler_throws_disposing_host_throws_AggregateException_containing_the_thrown_exception_and_SendAsync_throws_MessageDispatchingFailedException()
   {
      TueryHandlerThreadGate.ThrowPostPassThrough(_thrownException);
      //urgent: this seems to do some pretty strange async related things
      await InvokingAsync(() => Navigator.GetAsync(new MyTuery()))
                         .Must().ThrowAsync<MessageDispatchingFailedException>();
   }

   async Task AssertDisposingHostThrowsAggregateExceptionHierarchyContainingThrownExceptionAsANonAggregateException()
   {
      var exception = (await InvokingAsync(async Task () => await Host.DisposeAsync())
                            .Must().ThrowAsync<AggregateException>()).Which;
      var rootExceptions = exception.Flatten().InnerExceptions;
      rootExceptions.Must().Contain(_thrownException);
   }

   readonly IntentionalException _thrownException = new();
   class IntentionalException : Exception;
}
