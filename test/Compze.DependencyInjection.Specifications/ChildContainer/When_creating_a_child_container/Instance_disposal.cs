using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;
using Compze.Must.Assertions;

namespace Compze.DependencyInjection.Specifications.ChildContainer.When_creating_a_child_container;

public class Instance_disposal
{
   [DependencyInjectionContainerMatrix]
   public void disposing_child_does_not_dispose_parent_singletons()
   {
      var parentBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      parentBuilder.Registrar.Register(
         Singleton.For<IDisposableService>().CreatedBy(() => new DisposableService()));

      using var parent = parentBuilder.Build();
      var parentInstance = (DisposableService)parent.Resolve<IDisposableService>();

      var child = parent.CreateChildContainerBuilder().Build();
      var childInstance = child.Resolve<IDisposableService>();

      childInstance.Must().Be(parentInstance);

      child.Dispose();

      // After disposing the child, the parent's singleton must NOT be disposed
      parentInstance.IsDisposed.Must().BeFalse();

      // Parent should still be able to resolve it
      parent.Resolve<IDisposableService>().Must().Be(parentInstance);
   }

   [DependencyInjectionContainerMatrix]
   public void disposing_child_does_not_dispose_parent_async_disposable_singletons()
   {
      var parentBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      parentBuilder.Registrar.Register(
         Singleton.For<IAsyncDisposableService>().CreatedBy(() => new AsyncDisposableService()));

      using var parent = parentBuilder.Build();
      var parentInstance = parent.Resolve<IAsyncDisposableService>();

      var child = parent.CreateChildContainerBuilder().Build();
      var childInstance = child.Resolve<IAsyncDisposableService>();

      childInstance.Must().Be(parentInstance);

      child.Dispose();

      ((AsyncDisposableService)parentInstance).IsDisposed.Must().BeFalse();
   }
}
