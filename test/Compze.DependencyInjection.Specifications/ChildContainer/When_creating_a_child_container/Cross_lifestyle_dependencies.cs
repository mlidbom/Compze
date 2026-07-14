using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;
using Compze.Must.Assertions;

namespace Compze.DependencyInjection.Specifications.ChildContainer.When_creating_a_child_container;

public class Cross_lifestyle_dependencies
{
   [DependencyInjectionContainerMatrix]
   public void child_resolves_scoped_service_depending_on_parent_singleton()
   {
      var parentBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      parentBuilder.Registrar.Register(
         Singleton.For<ISingletonService>().CreatedBy(() => new SingletonService()),
         Scoped.For<IScopedService>().CreatedBy((ISingletonService s) => new ScopedServiceDependingOnSingleton(s))
      );

      using var parent = parentBuilder.Build();
      using var child = parent.CreateChildContainerBuilder().Build();
      using var scope = child.BeginScope();
      scope.Resolve<IScopedService>().Must().NotBeNull();
   }
}
