using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Hosting.Testing.Wiring;

public static class TestingContainerFactory
{
   public static IDependencyInjectionContainer CreateWithRegisteredServiceLocator(IComponentRegistrar registrar)
      => TestEnv.DIContainer.CreateWithRegisteredServiceLocator();
}
