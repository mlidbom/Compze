using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;

namespace Compze.DependencyInjection.Specifications.Cloning.When_cloning_a_container;

public class Multiple_clones
{
   [DependencyInjectionContainerMatrix]
   public void source_can_be_cloned_multiple_times()
   {
      using var source = DependencyInjectionContainerFactory.CreateContainer();
      source.Register(Singleton.For<ISingletonService>().CreatedBy(() => new SingletonService()));

      using var clone1 = source.Clone();
      using var clone2 = source.Clone();

      var instance1 = clone1.ServiceLocator.Resolve<ISingletonService>();
      var instance2 = clone2.ServiceLocator.Resolve<ISingletonService>();

      instance1.Must().NotBe(instance2);
   }
}
