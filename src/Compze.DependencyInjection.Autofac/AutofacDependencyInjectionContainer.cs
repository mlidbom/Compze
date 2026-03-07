using Autofac;
using Compze.Contracts;
using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Threading;

namespace Compze.DependencyInjection.Autofac;

public sealed class AutofacDependencyInjectionContainer : DependencyInjectionContainerBase, IServiceLocator, IServiceLocatorKernel, IAutofacContainerInternals
{
   readonly ContainerBuilder _containerBuilder = new();
   IContainer? _container;

   readonly AsyncLocal<ILifetimeScope?> _currentScope = new();

   public AutofacDependencyInjectionContainer(IComponentRegistrar? register = null) : base(register) {}

   protected override IDependencyInjectionContainer RegisterInContainer(ComponentRegistration[] registrations)
   {
      foreach(var registration in registrations)
      {
         if(registration.InstantiationSpec.SingletonInstance is {} instance)
         {
            registration.ServiceTypes.ForEach(serviceType =>
               _containerBuilder.RegisterInstance(instance).As(serviceType).ExternallyOwned());
         } else
         {
            _containerBuilder.Register(_ => registration.InstantiationSpec.RunFactoryMethod(this))
                             .As(registration.ServiceTypes.ToArray())
                             .WithCompzeLifestyle(registration.Lifestyle);
         }
      }

      return this;
   }

   readonly RunOnce _runVerifications = new();
   public override IServiceLocator ServiceLocator
   {
      get
      {
         if(_container == null)
         {
            _runVerifications.RunIfFirstCall(AssertLifeStyleCombinationsAreValid);
            _container = _containerBuilder.Build();
         }

         return this;
      }
   }

   protected override DependencyInjectionContainerBase CreateEmptyClone() =>
      new AutofacDependencyInjectionContainer(Register().Clone());

   protected override IReadOnlyList<Type> ContainerFacadeServiceTypes { get; } =
      [typeof(IDependencyInjectionContainer), typeof(IServiceLocator), typeof(AutofacDependencyInjectionContainer)];

   ILifetimeScope IAutofacContainerInternals.LifetimeScope => _container._assert().NotNull();

   ILifetimeScope CurrentScope() => _currentScope.Value ?? _container._assert().NotNull();

   protected override bool IsInScope() => _currentScope.Value != null;

   public TComponent Resolve<TComponent>() where TComponent : class
   {
      if(TryCreateTransientInstance(typeof(TComponent), this, out var transientInstance))
         return (TComponent)transientInstance;
      return CurrentScope().Resolve<TComponent>();
   }

   public object Resolve(Type serviceType)
   {
      if(TryCreateTransientInstance(serviceType, this, out var transientInstance))
         return transientInstance;
      return CurrentScope().Resolve(serviceType);
   }

   TComponent IServiceLocatorKernel.Resolve<TComponent>()
   {
      if(TryCreateTransientInstance(typeof(TComponent), this, out var transientInstance))
         return (TComponent)transientInstance;
      return CurrentScope().Resolve<TComponent>();
   }

   IDisposable IServiceLocator.BeginScope()
   {
      var parentScope = CurrentScope();
      var childScope = parentScope.BeginLifetimeScope();
      _currentScope.Value = childScope;

      return new ScopeDisposer(childScope, _currentScope, parentScope == _container ? null : parentScope);
   }

   public override void Dispose() => _container?.Dispose();
   public override async ValueTask DisposeAsync()
   {
      if(_container != null)
         await _container.DisposeAsync().caf();
   }

   sealed class ScopeDisposer(ILifetimeScope scope, AsyncLocal<ILifetimeScope?> currentScope, ILifetimeScope? parentScope) : IDisposable
   {
      readonly ILifetimeScope _scope = scope;
      readonly AsyncLocal<ILifetimeScope?> _currentScope = currentScope;
      readonly ILifetimeScope? _parentScope = parentScope;

      public void Dispose()
      {
         _scope.Dispose();
         _currentScope.Value = _parentScope;
      }
   }
}
