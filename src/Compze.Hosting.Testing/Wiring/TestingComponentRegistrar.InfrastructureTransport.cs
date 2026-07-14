using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE;
using Compze.Internals.Testing;
using Compze.Internals.Transport;
using Compze.Internals.Transport.AspNet;
using Compze.Internals.Transport.NamedPipes;

namespace Compze.Hosting.Testing.Wiring;

public static class TestingComponentRegistrarInfrastructureTransport
{
   ///<summary>
   /// Registers the transport infrastructure every Compze endpoint needs no matter what it speaks — for the current
   /// test's <see cref="Transport"/>: the client side of the infrastructure-query transport that endpoint discovery
   /// runs on, plus whatever that transport's client machinery requires (the shared <see cref="IHttpClientFactoryCE"/>
   /// and the infrastructure-query controller for HTTP; nothing extra for named pipes, whose transport servers answer
   /// infrastructure queries themselves). Guarded so that the Tessaging and Typermedia transport registrations can
   /// each demand it without conflicting when an endpoint hosts both.
   ///</summary>
   public static IComponentRegistrar CurrentTestsInfrastructureTransportIfNotRegistered(this IComponentRegistrar register)
   {
      register.CastTo<TestingComponentRegistrar>();
      if(register.IsRegistered<IInfrastructureQueryTransport>()) return register;

      return TestEnv.Transport switch
      {
         Transport.AspNetCore => register.HttpClientFactoryCE()
                                         .HttpInfrastructureQueryTransport()
                                         .Register(InfrastructureQueryController.RegisterWith),
         Transport.NamedPipes => register.NamedPipeInfrastructureQueryTransport(),
         _ => throw new ArgumentOutOfRangeException()
      };
   }
}
