namespace Compze.DependencyInjection.Abstractions;

/// <summary>
/// A built container. Composes <see cref="IRootResolver"/>, <see cref="IScopeFactory"/>, and child container creation.
/// Does not inherit resolution or scope creation — exposes them via properties.
/// </summary>
public interface IDependencyInjectionContainer : IDisposable, IAsyncDisposable
{
   IRootResolver RootResolver { get; }
   IScopeFactory ScopeFactory { get; }
   IContainerBuilder CreateCloneContainerBuilder();

   /// <summary>
   /// Creates a child container builder. Unlike <see cref="CreateCloneContainerBuilder"/>, all singletons delegate to the parent by default
   /// (same instance, not disposed by child). Scoped and transient registrations are copied (fresh instances in child scopes).
   /// Additional registrations can be added to the returned builder before calling <see cref="IContainerBuilder.Build"/>.
   /// </summary>
   IContainerBuilder CreateChildContainerBuilder();
}
