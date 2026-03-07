namespace Compze.DependencyInjection.Specifications.Cloning.When_cloning_a_container;

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

#pragma warning disable CS9113 // Parameter is unread.
class SingletonServiceDependingOnTransient(ITransientService _) : ISingletonService;
class ScopedServiceDependingOnTransient(ITransientService _) : IScopedService;
class ScopedServiceDependingOnSingleton(ISingletonService _) : IScopedService;
#pragma warning restore CS9113 // Parameter is unread.
