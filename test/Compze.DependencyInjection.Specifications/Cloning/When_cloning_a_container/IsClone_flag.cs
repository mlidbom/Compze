using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;

namespace Compze.DependencyInjection.Specifications.Cloning.When_cloning_a_container;

public class IsClone_flag
{
   [DependencyInjectionContainerMatrix]
   public void the_clone_is_marked_as_a_clone()
   {
      using var source = DependencyInjectionContainerFactory.CreateContainer();
      source.Register(Singleton.For<ISingletonService>().CreatedBy(() => new SingletonService()));

      source.IsClone.Must().BeFalse();

      using var clone = source.Clone();

      clone.IsClone.Must().BeTrue();
   }

   [DependencyInjectionContainerMatrix]
   public void the_source_is_not_marked_as_a_clone()
   {
      using var source = DependencyInjectionContainerFactory.CreateContainer();
      source.Register(Singleton.For<ISingletonService>().CreatedBy(() => new SingletonService()));

      using var clone = source.Clone();

      source.IsClone.Must().BeFalse();
   }
}
