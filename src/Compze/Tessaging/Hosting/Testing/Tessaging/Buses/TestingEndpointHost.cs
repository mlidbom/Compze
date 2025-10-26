using System;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tessaging.Implementation.Transport.Client.Implementation.Http;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Hosting.Testing.Tessaging.Buses;

public class TestingEndpointHost(IComponentRegistrar registrar, Func<IComponentRegistrar, IDependencyInjectionContainer> containerFactory) : TestingEndpointHostBase(registrar, containerFactory)
{
   public static ITestingEndpointHost Create(Func<IComponentRegistrar, IDependencyInjectionContainer> containerFactory)
      => new TestingEndpointHost(new TestingComponentRegistrar(), containerFactory);

   public override IEndpoint RegisterClientEndpointForRegisteredEndpoints() =>
      RegisterClientEndpoint(builder =>
      {
         builder.Container.Register()
                .CurrentTestsTransportMessagePoster();
      });
}