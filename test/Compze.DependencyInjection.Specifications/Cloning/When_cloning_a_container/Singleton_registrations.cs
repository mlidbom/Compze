using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;

namespace Compze.DependencyInjection.Specifications.Cloning.When_cloning_a_container;

public class Singleton_registrations
{
   [DependencyInjectionContainerMatrix]
   public void clone_resolves_a_different_singleton_instance_than_source()
   {
      using var source = DependencyInjectionContainerFactory.CreateContainer();
      source.Register(Singleton.For<ISingletonService>().CreatedBy(() => new SingletonService()));

      using var clone = source.Clone();

      var sourceInstance = source.ServiceLocator.Resolve<ISingletonService>();
      var cloneInstance = clone.ServiceLocator.Resolve<ISingletonService>();

      sourceInstance.Must().NotBe(cloneInstance);
   }

   [DependencyInjectionContainerMatrix]
   public void clone_returns_same_instance_on_repeated_resolves()
   {
      using var source = DependencyInjectionContainerFactory.CreateContainer();
      source.Register(Singleton.For<ISingletonService>().CreatedBy(() => new SingletonService()));

      using var clone = source.Clone();

      var first = clone.ServiceLocator.Resolve<ISingletonService>();
      var second = clone.ServiceLocator.Resolve<ISingletonService>();

      first.Must().Be(second);
   }

   [DependencyInjectionContainerMatrix]
   public void delegated_singleton_resolves_to_same_instance_as_source()
   {
      using var source = DependencyInjectionContainerFactory.CreateContainer();
      source.Register(
         Singleton.For<ISingletonService>().CreatedBy(() => new SingletonService()).DelegateToParentServiceLocatorWhenCloning()
      );

      var sourceInstance = source.ServiceLocator.Resolve<ISingletonService>();

      using var clone = source.Clone();

      var cloneInstance = clone.ServiceLocator.Resolve<ISingletonService>();

      sourceInstance.Must().Be(cloneInstance);
   }
}
