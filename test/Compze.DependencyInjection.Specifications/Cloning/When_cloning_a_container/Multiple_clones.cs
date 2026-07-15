using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;


namespace Compze.DependencyInjection.Specifications.Cloning.When_cloning_a_container;

public class Multiple_clones
{
   [DependencyInjectionContainerMatrix]
   public void source_can_be_cloned_multiple_times()
   {
      var sourceBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      sourceBuilder.Registrar.Register(Singleton.For<ISingletonService>().CreatedBy(() => new SingletonService()));

      using var source = sourceBuilder.Build();
      using var clone1 = source.CreateCloneContainerBuilder().Build();
      using var clone2 = source.CreateCloneContainerBuilder().Build();

      var instance1 = clone1.Resolve<ISingletonService>();
      var instance2 = clone2.Resolve<ISingletonService>();

      instance1.Must().NotBe(instance2);
   }
}
