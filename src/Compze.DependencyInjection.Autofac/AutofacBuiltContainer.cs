using Autofac;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.DependencyInjection.Autofac;

public sealed class AutofacBuiltContainer : BuiltContainerBase, IRootResolver, IScopeFactory, IAutofacContainerInternals
{
   readonly IContainer _container;

   internal AutofacBuiltContainer(IContainer container, IReadOnlyList<ComponentRegistration> registrations, IComponentRegistrar sourceRegistrar)
      : base(registrations, sourceRegistrar)
   {
      _container = container;
   }

   protected override ContainerBuilderBase CreateBuilderForClone(IComponentRegistrar clonedRegistrar) =>
      new AutofacContainerBuilder(clonedRegistrar);

   public object Resolve(Type serviceType) =>
      _container.Resolve(serviceType);

   public IServiceScope BeginScope()
   {
      var lifetimeScope = _container.BeginLifetimeScope();
      var scopeResolver = lifetimeScope.Resolve<ScopeResolverWrapper>();
      return new ServiceLocatorScope(scopeResolver, lifetimeScope);
   }

   IContainer IAutofacContainerInternals.Container => _container;

   public override void Dispose() => _container.Dispose();

   public override async ValueTask DisposeAsync() => await _container.DisposeAsync().caf();

   sealed class ServiceLocatorScope(IScopeResolver scopeResolver, ILifetimeScope lifetimeScope) : IServiceScope
   {
      readonly ILifetimeScope _lifetimeScope = lifetimeScope;
      public IScopeResolver Resolver { get; } = scopeResolver;

      public void Dispose() => _lifetimeScope.Dispose();
   }
}
