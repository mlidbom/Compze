using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;
using DryIoc;
using IScope = Compze.DependencyInjection.Abstractions.IScope;
using Compze.DependencyInjection.DryIoc.Private;

namespace Compze.DependencyInjection.DryIoc;

public sealed class DryIocContainer : DependencyInjectionContainer, IRootResolver, IScopeFactory, IDryIocContainerInternals
{
   readonly IContainer _container;
   bool _isDisposed;

   internal DryIocContainer(IContainer container, IReadOnlyList<ComponentRegistration> registrations, IComponentRegistrar sourceRegistrar)
      : base(registrations, sourceRegistrar) =>
      _container = container;

   public override DryIocContainerBuilder CreateCloneContainerBuilder() => (DryIocContainerBuilder)base.CreateCloneContainerBuilder();

   public override DryIocContainerBuilder CreateChildContainerBuilder() => (DryIocContainerBuilder)base.CreateChildContainerBuilder();

   protected override object ResolveCore(Type serviceType)
   {
      Contract.State.NotDisposed(_isDisposed, this);
      return _container.Resolve(serviceType);
   }

   protected override IEnumerable<object> ResolveSetCore(Type serviceType)
   {
      Contract.State.NotDisposed(_isDisposed, this);
      return (IEnumerable<object>)_container.Resolve(typeof(IEnumerable<>).MakeGenericType(serviceType));
   }

   public IScope BeginScope()
   {
      Contract.State.NotDisposed(_isDisposed, this);

      var scope = _container.OpenScope();
      var scopeResolver = scope.Resolve<ScopeResolver>();
      var tracker = scope.Resolve<DisposableTracker>();

      return new Scope(scopeResolver, scope, tracker);
   }

   IContainer IDryIocContainerInternals.Container => _container;

   public override void Dispose()
   {
      if(!_isDisposed)
      {
         _isDisposed = true;
         _container.Resolve<DisposableTracker>().DisposeAll();
         _container.Dispose();
      }
   }

   public override async ValueTask DisposeAsync()
   {
      if(!_isDisposed)
      {
         _isDisposed = true;
         await _container.Resolve<DisposableTracker>().DisposeAllAsync().ConfigureAwait(false);
         _container.Dispose();
      }
   }

   protected override ContainerBuilder CreateConcreteBuilder(IComponentRegistrar registrar) => new DryIocContainerBuilder(registrar);

   sealed class Scope : IScope
   {
      readonly IResolverContext _resolverContext;
      readonly DisposableTracker _tracker;

      public Scope(IScopeResolver scopeResolver, IResolverContext resolverContext, DisposableTracker tracker)
      {
         _resolverContext = resolverContext;
         _tracker = tracker;
         Resolver = scopeResolver;
      }

      public IScopeResolver Resolver { get; }

      public void Dispose()
      {
         _tracker.DisposeAll();
         _resolverContext.Dispose();
      }
   }
}
