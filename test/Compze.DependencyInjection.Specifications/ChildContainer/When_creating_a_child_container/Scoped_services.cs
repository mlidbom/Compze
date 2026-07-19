using Compze.DependencyInjection.Runtime;
using Compze.DependencyInjection.Runtime.Resolution;
using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.DependencyInjection.Wiring.Registration;
using Compze.Must;


namespace Compze.DependencyInjection.Specifications.ChildContainer.When_creating_a_child_container;

public class Scoped_services
{
   [DependencyInjectionContainerMatrix]
   public void child_can_resolve_scoped_services_within_a_scope()
   {
      var parentBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      parentBuilder.Registrar.Register(Scoped.For<IScopedService>().CreatedBy(() => new ScopedService()));

      using var parent = parentBuilder.Build();
      using var child = parent.CreateChildContainerBuilder().Build();

      using var scope = child.BeginScope();
      scope.Resolve<IScopedService>().Must().NotBeNull();
   }

   [DependencyInjectionContainerMatrix]
   public void scoped_instances_are_independent_between_parent_and_child()
   {
      var parentBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      parentBuilder.Registrar.Register(Scoped.For<IScopedService>().CreatedBy(() => new ScopedService()));

      using var parent = parentBuilder.Build();
      using var child = parent.CreateChildContainerBuilder().Build();

      IScopedService parentInstance;
      IScopedService childInstance;

      {
         using var parentScope = parent.BeginScope();
         parentInstance = parentScope.Resolve<IScopedService>();
      }

      {
         using var childScope = child.BeginScope();
         childInstance = childScope.Resolve<IScopedService>();
      }

      parentInstance.Must().NotBe(childInstance);
   }
}
