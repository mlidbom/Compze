using System;
using Compze.Messaging.Buses.Implementation;
using Compze.Testing;

namespace Compze.Messaging.Buses;

static class TestingHostExtensions
{
   public static TException AssertThatRunningScenarioThrowsBackendException<TException>(this ITestingEndpointHost @this, Action action) where TException : Exception
   {
      try
      {
         action();
      }
      catch(AggregateException exception) when(exception.InnerException is MessageDispatchingFailedException) {}

      return @this.AssertThrown<TException>();
   }

   public static (TException BackendException, MessageDispatchingFailedException FrontEndException) AssertThatRunningScenarioThrowsBackendAndClientException<TException>(this ITestingEndpointHost @this, Action action) where TException : Exception
   {
      var frontEndException = AssertThrows.Exception<MessageDispatchingFailedException>(action);

      return (@this.AssertThrown<TException>(), frontEndException);
   }
}