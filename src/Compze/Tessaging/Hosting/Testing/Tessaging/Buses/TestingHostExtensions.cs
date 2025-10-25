using System;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using FluentAssertions;

namespace Compze.Tessaging.Hosting.Testing.Tessaging.Buses;

public static class TestingHostExtensions
{
   public static (TException BackendException, TessageDispatchingFailedException FrontEndException) AssertThatRunningScenarioThrowsBackendAndClientException<TException>(this ITestingEndpointHost @this, Action action) where TException : Exception
   {
      var frontEndException = FluentActions.Invoking(action)
                                           .Should().Throw<TessageDispatchingFailedException>()
                                           .Which;

      return (@this.AssertThrown<TException>(), frontEndException);
   }
}