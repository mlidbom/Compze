using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;

namespace Compze.DependencyInjection.Specifications.Cloning.When_cloning_a_container;

public class Instance_disposal
{
   [DependencyInjectionContainerMatrix]
   public void Disposing_clone_does_not_dispose_delegated_singleton_instances()
   {
      var sourceBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      sourceBuilder.Registrar.Register(
         Singleton.For<IDisposableTracking>().CreatedBy(() => new DisposableTracking())
                  .DelegateToParentServiceLocatorWhenCloning());

      using var source = sourceBuilder.Build();
      var instance = source.Resolve<IDisposableTracking>();

      var clone = source.Clone().Build();
      var cloneInstance = clone.Resolve<IDisposableTracking>();

      cloneInstance.Must().Be(instance);

      clone.Dispose();

      // After disposing the clone, the shared instance must NOT be disposed
      ((DisposableTracking)instance).IsDisposed.Must().BeFalse();

      // Source should still be able to resolve it
      source.Resolve<IDisposableTracking>().Must().Be(instance);
   }

   [DependencyInjectionContainerMatrix]
   public void Disposing_clone_does_not_dispose_delegated_singleton_instance_that_is_async_disposable()
   {
      var sourceBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      sourceBuilder.Registrar.Register(
         Singleton.For<IAsyncDisposableTracking>().CreatedBy(() => new AsyncDisposableTracking())
                  .DelegateToParentServiceLocatorWhenCloning());

      using var source = sourceBuilder.Build();
      var instance = source.Resolve<IAsyncDisposableTracking>();

      var clone = source.Clone().Build();
      var cloneInstance = clone.Resolve<IAsyncDisposableTracking>();

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
