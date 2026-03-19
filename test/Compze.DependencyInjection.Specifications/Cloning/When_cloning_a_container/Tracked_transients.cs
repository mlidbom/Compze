using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;

namespace Compze.DependencyInjection.Specifications.Cloning.When_cloning_a_container;

public class Tracked_transients
{
   [DependencyInjectionContainerMatrix]
   public void clone_can_resolve_tracked_transient_services()
   {
      using var source = DependencyInjectionContainerFactory.CreateContainerBuilder();
      source.Registrar.Register(TrackedTransient.For<ITransientService>().CreatedBy(() => new TransientService()));

      using var clone = source.Clone();

      var first = clone.Build().Resolve<ITransientService>();
      var second = clone.Build().Resolve<ITransientService>();

      first.Must().NotBe(second);
   }

   [DependencyInjectionContainerMatrix]
   public void clone_disposes_its_own_tracked_transients_independently()
   {
      using var source = DependencyInjectionContainerFactory.CreateContainerBuilder();
      source.Registrar.Register(TrackedTransient.For<IDisposableService>().CreatedBy(() => new DisposableService()));

      var clone = source.Clone();
      var cloneInstance = (DisposableService)clone.Build().Resolve<IDisposableService>();

      cloneInstance.IsDisposed.Must().BeFalse();
      clone.Dispose();
      cloneInstance.IsDisposed.Must().BeTrue();
   }
}
