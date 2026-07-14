using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;
using Compze.Must.Assertions;

namespace Compze.DependencyInjection.Specifications.Cloning.When_cloning_a_container;

public class Tracked_transients
{
   [DependencyInjectionContainerMatrix]
   public void clone_can_resolve_tracked_transient_services()
   {
      var sourceBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      sourceBuilder.Registrar.Register(TrackedTransient.For<ITransientService>().CreatedBy(() => new TransientService()));

      using var source = sourceBuilder.Build();
      using var clone = source.CreateCloneContainerBuilder().Build();

      var first = clone.Resolve<ITransientService>();
      var second = clone.Resolve<ITransientService>();

      first.Must().NotBe(second);
   }

   [DependencyInjectionContainerMatrix]
   public void clone_disposes_its_own_tracked_transients_independently()
   {
      var sourceBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      sourceBuilder.Registrar.Register(TrackedTransient.For<IDisposableService>().CreatedBy(() => new DisposableService()));

      using var source = sourceBuilder.Build();
      var clone = source.CreateCloneContainerBuilder().Build();
      var cloneInstance = (DisposableService)clone.Resolve<IDisposableService>();

      cloneInstance.IsDisposed.Must().BeFalse();
      clone.Dispose();
      cloneInstance.IsDisposed.Must().BeTrue();
   }
}
