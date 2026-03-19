using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;

namespace Compze.DependencyInjection.Specifications.Cloning.When_cloning_a_container;

public class Scoped_services
{
   [DependencyInjectionContainerMatrix]
   public void clone_can_resolve_scoped_services_within_a_scope()
   {
      using var source = DependencyInjectionContainerFactory.CreateContainerBuilder();
      source.Registrar.Register(Scoped.For<IScopedService>().CreatedBy(() => new ScopedService()));

      using var clone = source.Clone();
      var container = clone.Build();

      using var scope = container.BeginScope();
      scope.Resolve<IScopedService>().Must().NotBeNull();
   }

   [DependencyInjectionContainerMatrix]
   public void scoped_instances_are_independent_between_source_and_clone()
   {
      using var source = DependencyInjectionContainerFactory.CreateContainerBuilder();
      source.Registrar.Register(Scoped.For<IScopedService>().CreatedBy(() => new ScopedService()));

      using var clone = source.Clone();

      IScopedService sourceInstance;
      IScopedService cloneInstance;

      {
         using var sourceScope = source.Build().BeginScope();
         sourceInstance = sourceScope.Resolve<IScopedService>();
      }

      {
         using var cloneScope = clone.Build().BeginScope();
         cloneInstance = cloneScope.Resolve<IScopedService>();
      }

      sourceInstance.Must().NotBe(cloneInstance);
   }
}
