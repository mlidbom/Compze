namespace Compze.DependencyInjection.Specifications.ChildContainer.When_creating_a_child_container;

interface ISingletonService;
class SingletonService : ISingletonService;

interface IScopedService;
class ScopedService : IScopedService;

interface ITransientService;
class TransientService : ITransientService;

interface IDisposableService;
class DisposableService : IDisposableService, IDisposable
{
   public bool IsDisposed { get; private set; }
   public void Dispose() => IsDisposed = true;
}

interface IAsyncDisposableService;
class AsyncDisposableService : IAsyncDisposableService, IAsyncDisposable
{
   public bool IsDisposed { get; private set; }
   public ValueTask DisposeAsync()
   {
      IsDisposed = true;
      return ValueTask.CompletedTask;
   }
}

interface IChildOnlyService;
class ChildOnlyService : IChildOnlyService;

#pragma warning disable CS9113 // Parameter is unread.
class ScopedServiceDependingOnSingleton(ISingletonService _) : IScopedService;
#pragma warning restore CS9113 // Parameter is unread.
