using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;

namespace Compze.DependencyInjection.Specifications.Cloning.When_cloning_a_container;

public class Untracked_transients
{
   [DependencyInjectionContainerMatrix]
   public void clone_can_resolve_untracked_transient_services()
   {
      using var source = DependencyInjectionContainerFactory.CreateContainer();
      source.Register(Transient.For<ITransientService>().CreatedBy(() => new TransientService()));

      using var clone = source.Clone();

      var first = clone.ServiceLocator.Resolve<ITransientService>();
      var second = clone.ServiceLocator.Resolve<ITransientService>();

      first.Must().NotBe(second);
   }
}
