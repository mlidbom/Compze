using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;

namespace Compze.DependencyInjection.Specifications.Cloning.When_cloning_a_container;

public class Cross_lifestyle_dependencies
{
   [DependencyInjectionContainerMatrix]
   public void clone_resolves_services_with_dependencies()
   {
      using var source = DependencyInjectionContainerFactory.CreateContainerBuilder();
      source.Registrar.Register(
         Singleton.For<ISingletonService>().CreatedBy(() => new SingletonService()),
         Scoped.For<IScopedService>().CreatedBy((ISingletonService s) => new ScopedServiceDependingOnSingleton(s))
      );

      using var clone = source.Clone();
      using var scope = clone.Build().BeginScope();
      scope.Resolve<IScopedService>().Must().NotBeNull();
   }
}
