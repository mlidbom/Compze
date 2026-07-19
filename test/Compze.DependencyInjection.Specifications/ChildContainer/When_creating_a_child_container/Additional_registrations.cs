using Compze.DependencyInjection.Runtime;
using Compze.DependencyInjection.Runtime.Resolution;
using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.DependencyInjection.Wiring.Registration;
using Compze.Must;


namespace Compze.DependencyInjection.Specifications.ChildContainer.When_creating_a_child_container;

public class Additional_registrations
{
   [DependencyInjectionContainerMatrix]
   public void child_can_resolve_services_registered_only_in_child()
   {
      var parentBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      parentBuilder.Registrar.Register(Singleton.For<ISingletonService>().CreatedBy(() => new SingletonService()));

      using var parent = parentBuilder.Build();

      var childBuilder = parent.CreateChildContainerBuilder();
      childBuilder.Registrar.Register(Singleton.For<IChildOnlyService>().CreatedBy(() => new ChildOnlyService()));

      using var child = childBuilder.Build();

      child.Resolve<IChildOnlyService>().Must().NotBeNull();
      child.Resolve<ISingletonService>().Must().Be(parent.Resolve<ISingletonService>());
   }

   [DependencyInjectionContainerMatrix]
   public void child_can_resolve_additional_scoped_service()
   {
      var parentBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      parentBuilder.Registrar.Register(Singleton.For<ISingletonService>().CreatedBy(() => new SingletonService()));

      using var parent = parentBuilder.Build();

      var childBuilder = parent.CreateChildContainerBuilder();
      childBuilder.Registrar.Register(Scoped.For<IChildOnlyService>().CreatedBy(() => new ChildOnlyService()));

      using var child = childBuilder.Build();
      using var scope = child.BeginScope();
      scope.Resolve<IChildOnlyService>().Must().NotBeNull();
   }

   [DependencyInjectionContainerMatrix]
   public void child_additional_service_can_depend_on_parent_singleton()
   {
      var parentBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      parentBuilder.Registrar.Register(Singleton.For<ISingletonService>().CreatedBy(() => new SingletonService()));

      using var parent = parentBuilder.Build();

      var childBuilder = parent.CreateChildContainerBuilder();
      childBuilder.Registrar.Register(
         Scoped.For<IScopedService>().CreatedBy((ISingletonService s) => new ScopedServiceDependingOnSingleton(s)));

      using var child = childBuilder.Build();
      using var scope = child.BeginScope();
      scope.Resolve<IScopedService>().Must().NotBeNull();
   }
}
