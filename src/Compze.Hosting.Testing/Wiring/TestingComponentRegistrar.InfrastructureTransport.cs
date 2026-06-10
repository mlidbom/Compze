using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE;
using Compze.Internals.Transport;
using Compze.Internals.Transport.AspNet;

namespace Compze.Hosting.Testing.Wiring;

public static class TestingComponentRegistrarInfrastructureTransport
{
   ///<summary>
   /// Registers the transport infrastructure every Compze endpoint needs regardless of paradigm: the shared
   /// <see cref="IHttpClientFactoryCE"/>, and the client and server sides of the infrastructure-query transport
   /// that endpoint discovery runs on. Guarded so that paradigm transport registrations can each demand it
   /// without conflicting when an endpoint hosts more than one paradigm.
   ///</summary>
   public static IComponentRegistrar CurrentTestsInfrastructureTransportIfNotRegistered(this IComponentRegistrar register)
   {
      register.CastTo<TestingComponentRegistrar>();
      if(register.IsRegistered<IHttpClientFactoryCE>()) return register;

      return register.HttpClientFactoryCE()
                     .HttpInfrastructureQueryTransport()
                     .Register(InfrastructureQueryController.RegisterWith);
   }
}
