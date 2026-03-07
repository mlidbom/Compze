using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;

namespace Compze.DependencyInjection.Specifications.Cloning.When_cloning_a_container;

public class Instance_disposal
{
   [DependencyInjectionContainerMatrix]
   public void Disposing_clone_does_not_dispose_delegated_singleton_instances()
   {
      using var source = DependencyInjectionContainerFactory.CreateContainer();
      source.Register(
         Singleton.For<IDisposableTracking>().CreatedBy(() => new DisposableTracking())
                  .DelegateToParentServiceLocatorWhenCloning());

      var sourceLocator = source.ServiceLocator;
      var instance = sourceLocator.Resolve<IDisposableTracking>();

      var clone = source.Clone();
      var cloneInstance = clone.ServiceLocator.Resolve<IDisposableTracking>();

      cloneInstance.Must().Be(instance);

      clone.Dispose();

      // After disposing the clone, the shared instance must NOT be disposed
      ((DisposableTracking)instance).IsDisposed.Must().BeFalse();

      // Source should still be able to resolve it
      sourceLocator.Resolve<IDisposableTracking>().Must().Be(instance);
   }

   [DependencyInjectionContainerMatrix]
   public void Disposing_clone_does_not_dispose_delegated_singleton_instance_that_is_async_disposable()
   {
      using var source = DependencyInjectionContainerFactory.CreateContainer();
      source.Register(
         Singleton.For<IAsyncDisposableTracking>().CreatedBy(() => new AsyncDisposableTracking())
                  .DelegateToParentServiceLocatorWhenCloning());

      var sourceLocator = source.ServiceLocator;
      var instance = sourceLocator.Resolve<IAsyncDisposableTracking>();

      var clone = source.Clone();
      var cloneInstance = clone.ServiceLocator.Resolve<IAsyncDisposableTracking>();

      cloneInstance.Must().Be(instance);

      clone.Dispose();

      ((AsyncDisposableTracking)instance).IsDisposed.Must().BeFalse();
   }
}

interface IDisposableTracking;
class DisposableTracking : IDisposableTracking, IDisposable
{
   public bool IsDisposed { get; private set; }
   public void Dispose() => IsDisposed = true;
}

interface IAsyncDisposableTracking;
class AsyncDisposableTracking : IAsyncDisposableTracking, IDisposable, IAsyncDisposable
{
   public bool IsDisposed { get; private set; }
   public void Dispose() => IsDisposed = true;
   public ValueTask DisposeAsync() { IsDisposed = true; return ValueTask.CompletedTask; }
}
