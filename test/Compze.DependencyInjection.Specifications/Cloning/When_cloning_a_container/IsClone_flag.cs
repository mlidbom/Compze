using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;
using Compze.Must.Assertions;

namespace Compze.DependencyInjection.Specifications.Cloning.When_cloning_a_container;

public class IsClone_flag
{
   [DependencyInjectionContainerMatrix]
   public void the_clone_is_marked_as_a_clone()
   {
      var sourceBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      sourceBuilder.Registrar.Register(Singleton.For<ISingletonService>().CreatedBy(() => new SingletonService()));

      sourceBuilder.Registrar.IsClone.Must().BeFalse();

      using var source = sourceBuilder.Build();
      var cloneBuilder = source.CreateCloneContainerBuilder();

      cloneBuilder.Registrar.IsClone.Must().BeTrue();
   }

   [DependencyInjectionContainerMatrix]
   public void the_source_is_not_marked_as_a_clone()
   {
      var sourceBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      sourceBuilder.Registrar.Register(Singleton.For<ISingletonService>().CreatedBy(() => new SingletonService()));

      using var source = sourceBuilder.Build();
      _ = source.CreateCloneContainerBuilder();

      sourceBuilder.Registrar.IsClone.Must().BeFalse();
   }
}
