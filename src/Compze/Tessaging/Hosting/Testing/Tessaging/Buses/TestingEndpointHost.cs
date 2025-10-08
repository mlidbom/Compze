using System;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Hosting.Testing.Tessaging.Buses;

public class TestingEndpointHost(IRunMode mode, Func<IRunMode, IDependencyInjectionContainer> containerFactory) : TestingEndpointHostBase(mode, containerFactory)
{
   public static ITestingEndpointHost Create(Func<IRunMode, IDependencyInjectionContainer> containerFactory)
      => new TestingEndpointHost(new RunMode(isTesting: true), containerFactory);
}