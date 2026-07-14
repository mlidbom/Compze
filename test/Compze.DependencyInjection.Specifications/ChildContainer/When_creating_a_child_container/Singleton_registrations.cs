using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;
using Compze.Must.Assertions;

namespace Compze.DependencyInjection.Specifications.ChildContainer.When_creating_a_child_container;

public class Singleton_registrations
{
   [DependencyInjectionContainerMatrix]
   public void child_resolves_same_singleton_instance_as_parent()
   {
      var parentBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      parentBuilder.Registrar.Register(Singleton.For<ISingletonService>().CreatedBy(() => new SingletonService()));

      using var parent = parentBuilder.Build();
      using var child = parent.CreateChildContainerBuilder().Build();

      var parentInstance = parent.Resolve<ISingletonService>();
      var childInstance = child.Resolve<ISingletonService>();

      childInstance.Must().Be(parentInstance);
   }

   [DependencyInjectionContainerMatrix]
   public void child_returns_same_singleton_on_repeated_resolves()
   {
      var parentBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      parentBuilder.Registrar.Register(Singleton.For<ISingletonService>().CreatedBy(() => new SingletonService()));

      using var parent = parentBuilder.Build();
      using var child = parent.CreateChildContainerBuilder().Build();

      var first = child.Resolve<ISingletonService>();
      var second = child.Resolve<ISingletonService>();

      first.Must().Be(second);
   }

   [DependencyInjectionContainerMatrix]
   public void singleton_with_DelegateToParent_also_delegates_in_child()
   {
      var parentBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      parentBuilder.Registrar.Register(
         Singleton.For<ISingletonService>().DelegateToParentServiceLocatorWhenCloning().CreatedBy(() => new SingletonService())
      );

      using var parent = parentBuilder.Build();
      using var child = parent.CreateChildContainerBuilder().Build();

      var parentInstance = parent.Resolve<ISingletonService>();
      var childInstance = child.Resolve<ISingletonService>();

      childInstance.Must().Be(parentInstance);
   }
}
