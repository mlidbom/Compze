using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;
using Compze.Must.Assertions;

namespace Compze.DependencyInjection.Specifications.Cloning.When_cloning_a_container;

public class Singleton_registrations
{
   [DependencyInjectionContainerMatrix]
   public void clone_resolves_a_different_singleton_instance_than_source()
   {
      var sourceBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      sourceBuilder.Registrar.Register(Singleton.For<ISingletonService>().CreatedBy(() => new SingletonService()));

      using var source = sourceBuilder.Build();
      using var clone = source.CreateCloneContainerBuilder().Build();

      var sourceInstance = source.Resolve<ISingletonService>();
      var cloneInstance = clone.Resolve<ISingletonService>();

      sourceInstance.Must().NotBe(cloneInstance);
   }

   [DependencyInjectionContainerMatrix]
   public void clone_returns_same_instance_on_repeated_resolves()
   {
      var sourceBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      sourceBuilder.Registrar.Register(Singleton.For<ISingletonService>().CreatedBy(() => new SingletonService()));

      using var source = sourceBuilder.Build();
      using var clone = source.CreateCloneContainerBuilder().Build();

      var first = clone.Resolve<ISingletonService>();
      var second = clone.Resolve<ISingletonService>();

      first.Must().Be(second);
   }

   [DependencyInjectionContainerMatrix]
   public void delegated_singleton_resolves_to_same_instance_as_source()
   {
      var sourceBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      sourceBuilder.Registrar.Register(
         Singleton.For<ISingletonService>().DelegateToParentServiceLocatorWhenCloning().CreatedBy(() => new SingletonService())
      );

      using var source = sourceBuilder.Build();
      var sourceInstance = source.Resolve<ISingletonService>();

      using var clone = source.CreateCloneContainerBuilder().Build();
      var cloneInstance = clone.Resolve<ISingletonService>();

      sourceInstance.Must().Be(cloneInstance);
   }
}
