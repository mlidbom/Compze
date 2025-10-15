using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Hosting.Testing.DependencyInjection;

public static class TestingContainerFactory
{
   public static IDependencyInjectionContainer Create(IRunMode runMode) 
      => TestEnv.DIContainer.CreateWithRegisteredServiceLocator(runMode);
}