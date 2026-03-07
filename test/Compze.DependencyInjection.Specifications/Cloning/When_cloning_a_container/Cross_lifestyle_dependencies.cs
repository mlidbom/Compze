using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;

namespace Compze.DependencyInjection.Specifications.Cloning.When_cloning_a_container;

public class Cross_lifestyle_dependencies
{
   [DependencyInjectionContainerMatrix]
   public void clone_resolves_services_with_dependencies()
   {
      using var source = DependencyInjectionContainerFactory.CreateContainer();
      source.Register(
         Singleton.For<ISingletonService>().CreatedBy(() => new SingletonService()),
         Scoped.For<IScopedService>().CreatedBy((ISingletonService s) => new ScopedServiceDependingOnSingleton(s))
      );

      using var clone = source.Clone();
      using(clone.ServiceLocator.BeginScope())
      {
         clone.ServiceLocator.Resolve<IScopedService>().Must().NotBeNull();
      }
   }
}
