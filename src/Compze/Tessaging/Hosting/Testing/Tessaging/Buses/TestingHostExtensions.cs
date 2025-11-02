using System;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Utilities.Testing.Fluent;
using static Compze.Utilities.Testing.Fluent.MustActions;

namespace Compze.Tessaging.Hosting.Testing.Tessaging.Buses;

public static class TestingHostExtensions
{
   public static (TException BackendException, TessageDispatchingFailedException FrontEndException) AssertThatRunningScenarioThrowsBackendAndClientException<TException>(this ITestingEndpointHost @this, Action action) where TException : Exception
   {
      var frontEndException = Invoking(action)
                                           .Must().Throw<TessageDispatchingFailedException>()
                                           .Which;

      return (@this.AssertThrown<TException>(), frontEndException);
   }
}