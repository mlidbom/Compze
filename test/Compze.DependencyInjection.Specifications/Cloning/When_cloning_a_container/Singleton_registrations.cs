using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;

namespace Compze.DependencyInjection.Specifications.Cloning.When_cloning_a_container;

public class Singleton_registrations
{
   [DependencyInjectionContainerMatrix]
   public void clone_resolves_a_different_singleton_instance_than_source()
   {
      using var source = DependencyInjectionContainerFactory.CreateContainerBuilder();
      source.Registrar.Register(Singleton.For<ISingletonService>().CreatedBy(() => new SingletonService()));

      using var clone = source.Clone();

      var sourceInstance = source.Build().Resolve<ISingletonService>();
      var cloneInstance = clone.Build().Resolve<ISingletonService>();

      sourceInstance.Must().NotBe(cloneInstance);
   }

   [DependencyInjectionContainerMatrix]
   public void clone_returns_same_instance_on_repeated_resolves()
   {
      using var source = DependencyInjectionContainerFactory.CreateContainerBuilder();
      source.Registrar.Register(Singleton.For<ISingletonService>().CreatedBy(() => new SingletonService()));

      using var clone = source.Clone();

      var first = clone.Build().Resolve<ISingletonService>();
      var second = clone.Build().Resolve<ISingletonService>();

      first.Must().Be(second);
   }

   [DependencyInjectionContainerMatrix]
   public void delegated_singleton_resolves_to_same_instance_as_source()
   {
      using var source = DependencyInjectionContainerFactory.CreateContainerBuilder();
      source.Registrar.Register(
         Singleton.For<ISingletonService>().CreatedBy(() => new SingletonService()).DelegateToParentServiceLocatorWhenCloning()
      );

      var sourceInstance = source.Build().Resolve<ISingletonService>();

      using var clone = source.Clone();

      var cloneInstance = clone.Build().Resolve<ISingletonService>();

      sourceInstance.Must().Be(cloneInstance);
   }
}
