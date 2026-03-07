using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;

namespace Compze.DependencyInjection.Specifications.Cloning.When_cloning_a_container;

public class Scoped_services
{
   [DependencyInjectionContainerMatrix]
   public void clone_can_resolve_scoped_services_within_a_scope()
   {
      using var source = DependencyInjectionContainerFactory.CreateContainer();
      source.Register(Scoped.For<IScopedService>().CreatedBy(() => new ScopedService()));

      using var clone = source.Clone();
      var serviceLocator = clone.ServiceLocator;

      using(serviceLocator.BeginScope())
      {
         serviceLocator.Resolve<IScopedService>().Must().NotBeNull();
      }
   }

   [DependencyInjectionContainerMatrix]
   public void scoped_instances_are_independent_between_source_and_clone()
   {
      using var source = DependencyInjectionContainerFactory.CreateContainer();
      source.Register(Scoped.For<IScopedService>().CreatedBy(() => new ScopedService()));

      using var clone = source.Clone();

      IScopedService sourceInstance;
      IScopedService cloneInstance;

      using(source.ServiceLocator.BeginScope())
      {
         sourceInstance = source.ServiceLocator.Resolve<IScopedService>();
      }

      using(clone.ServiceLocator.BeginScope())
      {
         cloneInstance = clone.ServiceLocator.Resolve<IScopedService>();
      }

      sourceInstance.Must().NotBe(cloneInstance);
   }
}
