using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;

namespace Compze.DependencyInjection.Specifications.Cloning.When_cloning_a_container;

public class Scoped_services
{
   [DependencyInjectionContainerMatrix]
   public void clone_can_resolve_scoped_services_within_a_scope()
   {
      var sourceBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      sourceBuilder.Registrar.Register(Scoped.For<IScopedService>().CreatedBy(() => new ScopedService()));

      using var source = sourceBuilder.Build();
      using var clone = source.Clone().Build();

      using var scope = clone.BeginScope();
      scope.Resolve<IScopedService>().Must().NotBeNull();
   }

   [DependencyInjectionContainerMatrix]
   public void scoped_instances_are_independent_between_source_and_clone()
   {
      var sourceBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      sourceBuilder.Registrar.Register(Scoped.For<IScopedService>().CreatedBy(() => new ScopedService()));

      using var source = sourceBuilder.Build();
      using var clone = source.Clone().Build();

      IScopedService sourceInstance;
      IScopedService cloneInstance;

      {
         using var sourceScope = source.BeginScope();
         sourceInstance = sourceScope.Resolve<IScopedService>();
      }

      {
         using var cloneScope = clone.BeginScope();
         cloneInstance = cloneScope.Resolve<IScopedService>();
      }

      sourceInstance.Must().NotBe(cloneInstance);
   }
}
