using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;
using LightInject;
using IScope = Compze.DependencyInjection.Abstractions.IScope;
using LightInjectScope = global::LightInject.Scope;
using Compze.DependencyInjection.LightInject._private;

namespace Compze.DependencyInjection.LightInject;

public sealed class LightInjectContainer : DependencyInjectionContainer, IRootResolver, IScopeFactory, ILightInjectContainerInternals
{
   readonly ServiceContainer _container;
   readonly DisposableTracker _rootTransientTracker = new();
   readonly IReadOnlyDictionary<Type, IReadOnlyList<string>> _memberNamesByServiceType;
   bool _isDisposed;

   internal LightInjectContainer(ServiceContainer container, IReadOnlyList<ComponentRegistration> registrations, IComponentRegistrar sourceRegistrar)
      : base(registrations, sourceRegistrar)
   {
      _container = container;
      _memberNamesByServiceType = LightInjectComponentSetNaming.ComputeMemberNamesByServiceType(registrations);
   }

   protected override LightInjectContainerBuilder CreateCloneContainerBuilder() => (LightInjectContainerBuilder)base.CreateCloneContainerBuilder();

   public override LightInjectContainerBuilder CreateChildContainerBuilder() => (LightInjectContainerBuilder)base.CreateChildContainerBuilder();

   protected override object ResolveCore(Type serviceType)
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

   protected override IEnumerable<object> ResolveSetCore(Type serviceType)
   {
      Contract.State.NotDisposed(_isDisposed, this);
      LightInjectContainerBuilder.CurrentTrackedTransientTracker.Value = _rootTransientTracker;
      try
      {
         // Materialized eagerly, not returned lazily: construction of any TrackedTransient member must happen while the
         // tracker is still set, so it gets registered for disposal — a deferred enumeration after this method returns
         // would run with the tracker already cleared below.
         return LightInjectContainerBuilder.ResolveComponentSetMembers(_container, _memberNamesByServiceType, serviceType).ToArray();
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
      var scopeResolver = new LightInjectScopeResolver(scope, scopeTracker, _memberNamesByServiceType);
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

   sealed class LightInjectScopeResolver(LightInjectScope scope, DisposableTracker tracker, IReadOnlyDictionary<Type, IReadOnlyList<string>> memberNamesByServiceType) : IScopeResolver
   {
      readonly LightInjectScope _scope = scope;
      readonly DisposableTracker _tracker = tracker;
      readonly IReadOnlyDictionary<Type, IReadOnlyList<string>> _memberNamesByServiceType = memberNamesByServiceType;

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

      public IEnumerable<object> ResolveSet(Type serviceType)
      {
         LightInjectContainerBuilder.CurrentTrackedTransientTracker.Value = _tracker;
         try
         {
            // Materialized eagerly — see the equivalent note on LightInjectContainer.ResolveSetCore.
            return LightInjectContainerBuilder.ResolveComponentSetMembers(_scope, _memberNamesByServiceType, serviceType).ToArray();
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
