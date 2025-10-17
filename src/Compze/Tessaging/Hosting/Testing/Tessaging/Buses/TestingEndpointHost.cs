using System;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Hosting.Testing.Tessaging.Buses;

public class TestingEndpointHost(IComponentRegistrar registrar, Func<IComponentRegistrar, IDependencyInjectionContainer> containerFactory) : TestingEndpointHostBase(registrar, containerFactory)
{
   public static ITestingEndpointHost Create(Func<IComponentRegistrar, IDependencyInjectionContainer> containerFactory)
      => new TestingEndpointHost(new TestingComponentRegistrar(), containerFactory);
}