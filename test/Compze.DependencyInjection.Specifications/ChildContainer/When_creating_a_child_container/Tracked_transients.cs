using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;
using Compze.Must.Assertions;

namespace Compze.DependencyInjection.Specifications.ChildContainer.When_creating_a_child_container;

public class Tracked_transients
{
   [DependencyInjectionContainerMatrix]
   public void child_can_resolve_tracked_transient_services()
   {
      var parentBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      parentBuilder.Registrar.Register(TrackedTransient.For<ITransientService>().CreatedBy(() => new TransientService()));

      using var parent = parentBuilder.Build();
      using var child = parent.CreateChildContainerBuilder().Build();

      var first = child.Resolve<ITransientService>();
      var second = child.Resolve<ITransientService>();

      first.Must().NotBe(second);
   }

   [DependencyInjectionContainerMatrix]
   public void child_disposes_its_own_tracked_transients_independently()
   {
      var parentBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      parentBuilder.Registrar.Register(TrackedTransient.For<IDisposableService>().CreatedBy(() => new DisposableService()));

      using var parent = parentBuilder.Build();
      var child = parent.CreateChildContainerBuilder().Build();
      var childInstance = (DisposableService)child.Resolve<IDisposableService>();

      childInstance.IsDisposed.Must().BeFalse();
      child.Dispose();
      childInstance.IsDisposed.Must().BeTrue();
   }
}
