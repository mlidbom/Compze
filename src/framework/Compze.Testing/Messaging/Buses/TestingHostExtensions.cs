using System;
using Compze.Tessaging.Buses;
using Compze.Tessaging.Buses.Implementation;
using FluentAssertions;

namespace Compze.Testing.Messaging.Buses;

public static class TestingHostExtensions
{
   public static (TException BackendException, MessageDispatchingFailedException FrontEndException) AssertThatRunningScenarioThrowsBackendAndClientException<TException>(this ITestingEndpointHost @this, Action action) where TException : Exception
   {
      var frontEndException = FluentActions.Invoking(action)
                                           .Should().Throw<MessageDispatchingFailedException>()
                                           .Which;

      return (@this.AssertThrown<TException>(), frontEndException);
   }
}