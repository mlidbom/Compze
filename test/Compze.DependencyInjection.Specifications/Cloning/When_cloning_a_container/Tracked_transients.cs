using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;

namespace Compze.DependencyInjection.Specifications.Cloning.When_cloning_a_container;

public class Tracked_transients
{
   [DependencyInjectionContainerMatrix]
   public void clone_can_resolve_tracked_transient_services()
   {
      using var source = DependencyInjectionContainerFactory.CreateContainer();
      source.Register(TrackedTransient.For<ITransientService>().CreatedBy(() => new TransientService()));

      using var clone = source.Clone();

      var first = clone.ServiceLocator.Resolve<ITransientService>();
      var second = clone.ServiceLocator.Resolve<ITransientService>();

      first.Must().NotBe(second);
   }

   [DependencyInjectionContainerMatrix]
   public void clone_disposes_its_own_tracked_transients_independently()
   {
      using var source = DependencyInjectionContainerFactory.CreateContainer();
      source.Register(TrackedTransient.For<IDisposableService>().CreatedBy(() => new DisposableService()));

      var clone = source.Clone();
      var cloneInstance = (DisposableService)clone.ServiceLocator.Resolve<IDisposableService>();

      cloneInstance.IsDisposed.Must().BeFalse();
      clone.Dispose();
      cloneInstance.IsDisposed.Must().BeTrue();
   }
}
