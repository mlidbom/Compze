using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;
using LightInject;
using IScope = Compze.DependencyInjection.Abstractions.IScope;
using LightInjectScope = global::LightInject.Scope;

namespace Compze.DependencyInjection.LightInject;

public sealed class LightInjectContainer : DependencyInjectionContainer, IRootResolver, IScopeFactory, ILightInjectContainerInternals
{
   readonly ServiceContainer _container;
   readonly DisposableTracker _rootTransientTracker = new();
   bool _isDisposed;

   internal LightInjectContainer(ServiceContainer container, IReadOnlyList<ComponentRegistration> registrations, IComponentRegistrar sourceRegistrar)
      : base(registrations, sourceRegistrar) =>
      _container = container;

   public override LightInjectContainerBuilder CreateCloneContainerBuilder() => (LightInjectContainerBuilder)base.CreateCloneContainerBuilder();

   public override LightInjectContainerBuilder CreateChildContainerBuilder() => (LightInjectContainerBuilder)base.CreateChildContainerBuilder();

   public object Resolve(Type serviceType)
   {
      Contract.State.NotDisposed(_isDisposed, this);
      LightInjectContainerBuilder.CurrentTrackedTransientTracker.Value = _rootTransientTracker;
      try
      {
         return _container.GetInstance(serviceType);
      }
      finally
      {
         LightInjectContainerBuilder.CurrentTrackedTransientTracker.Value = null;
      }
   }

   public IScope BeginScope()
   {
      Contract.State.NotDisposed(_isDisposed, this);

      var scope = _container.BeginScope();
      var scopeTracker = new DisposableTracker();
      var scopeResolver = new LightInjectScopeResolver(scope, scopeTracker);
      return new CompzeScope(scopeResolver, scope, scopeTracker);
   }

   IServiceContainer ILightInjectContainerInternals.ServiceContainer => _container;

   public override void Dispose()
   {
      if(!_isDisposed)
      {
         _isDisposed = true;
         _rootTransientTracker.DisposeAll();
         _container.Dispose();
      }
   }

   public override async ValueTask DisposeAsync()
   {
      if(!_isDisposed)
      {
         _isDisposed = true;
         await _rootTransientTracker.DisposeAllAsync().ConfigureAwait(false);
         _container.Dispose();
      }
   }

   protected override ContainerBuilder CreateConcreteBuilder(IComponentRegistrar registrar) => new LightInjectContainerBuilder(registrar);

   sealed class LightInjectScopeResolver(LightInjectScope scope, DisposableTracker tracker) : IScopeResolver
   {
      readonly LightInjectScope _scope = scope;
      readonly DisposableTracker _tracker = tracker;

      public object Resolve(Type serviceType)
      {
         LightInjectContainerBuilder.CurrentTrackedTransientTracker.Value = _tracker;
         try
         {
            return _scope.GetInstance(serviceType);
         }
         finally
         {
            LightInjectContainerBuilder.CurrentTrackedTransientTracker.Value = null;
         }
      }
   }

   sealed class CompzeScope : IScope
   {
      readonly LightInjectScope _lightInjectScope;
      readonly DisposableTracker _tracker;

      public CompzeScope(IScopeResolver scopeResolver, LightInjectScope lightInjectScope, DisposableTracker tracker)
      {
         _lightInjectScope = lightInjectScope;
         _tracker = tracker;
         Resolver = scopeResolver;
      }

      public IScopeResolver Resolver { get; }

      public void Dispose()
      {
         _tracker.DisposeAll();
         _lightInjectScope.Dispose();
      }
   }
}
