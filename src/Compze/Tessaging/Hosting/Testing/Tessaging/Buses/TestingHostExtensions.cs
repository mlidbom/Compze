using System;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Tessaging.Implementation.MessageHandling.Dispatching;
using FluentAssertions;

namespace Compze.Tessaging.Hosting.Testing.Tessaging.Buses;

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