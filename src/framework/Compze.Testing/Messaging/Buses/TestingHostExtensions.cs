using System;
using Compze.Messaging.Buses;
using Compze.Messaging.Buses.Implementation;
using FluentAssertions;

namespace Compze.Testing.Messaging.Buses;

public static class TestingHostExtensions
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
      var frontEndException = FluentActions.Invoking(action)
                                           .Should().Throw<MessageDispatchingFailedException>()
                                           .Which;

      return (@this.AssertThrown<TException>(), frontEndException);
   }
}