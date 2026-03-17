using Autofac;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Threading;

namespace Compze.DependencyInjection.Autofac;

public sealed class AutofacDependencyInjectionContainer(IComponentRegistrar? register = null) : DependencyInjectionContainerBase(register), IServiceLocator, IServiceLocatorKernel, IAutofacContainerInternals
{
   readonly ContainerBuilder _containerBuilder = new();
   IContainer? _container;

   readonly RunOnce _registerScopedKernel = new();

   protected override IDependencyInjectionContainer RegisterInContainer(ComponentRegistration[] registrations)
   {
      _registerScopedKernel.RunIfFirstCall(() =>
      {
         _containerBuilder.Register(ctx =>
         {
            var scope = ctx.Resolve<ILifetimeScope>();
            return new ScopedKernel(this, scope.Resolve);
         }).InstancePerLifetimeScope();
         _containerBuilder.Register(ctx => (IScopeServiceLocator)ctx.Resolve<ScopedKernel>())
                          .As<IScopeServiceLocator>()
                          .InstancePerLifetimeScope();
      });

      foreach(var registration in registrations)
      {
         if(registration.InstantiationSpec.SingletonInstance is {} instance)
         {
            registration.ServiceTypes.ForEach(serviceType =>
               _containerBuilder.RegisterInstance(instance).As(serviceType).ExternallyOwned());
         } else if(registration.Lifestyle == Lifestyle.Singleton)
         {
            _containerBuilder.Register(_ => registration.InstantiationSpec.RunFactoryMethod(this))
                             .As(registration.ServiceTypes.ToArray())
                             .WithCompzeLifestyle(registration.Lifestyle);
         } else
         {
            _containerBuilder.Register(ctx => registration.InstantiationSpec.RunFactoryMethod(ctx.Resolve<ScopedKernel>()))
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
   ContainerBuilder IAutofacContainerInternals.ContainerBuilder => _containerBuilder;

   public TComponent Resolve<TComponent>() where TComponent : class
   {
      if(TryCreateTransientInstance(typeof(TComponent), this, out var transientInstance))
         return (TComponent)transientInstance;
      return _container._assert().NotNull().Resolve<TComponent>();
   }

   public object Resolve(Type serviceType)
   {
      if(TryCreateTransientInstance(serviceType, this, out var transientInstance))
         return transientInstance;
      return _container._assert().NotNull().Resolve(serviceType);
   }

   TComponent IServiceLocatorKernel.Resolve<TComponent>()
   {
      if(TryCreateTransientInstance(typeof(TComponent), this, out var transientInstance))
         return (TComponent)transientInstance;
      return _container._assert().NotNull().Resolve<TComponent>();
   }

   IServiceLocatorScope IServiceLocator.BeginScope()
   {
      var lifetimeScope = _container._assert().NotNull().BeginLifetimeScope();
      var scopedKernel = lifetimeScope.Resolve<ScopedKernel>();
      return new ServiceLocatorScope(this, scopedKernel, lifetimeScope);
   }

   public override void Dispose() => _container?.Dispose();
   public override async ValueTask DisposeAsync()
   {
      if(_container != null)
         await _container.DisposeAsync().caf();
   }

   sealed class ServiceLocatorScope(AutofacDependencyInjectionContainer container, IScopeServiceLocator scopedKernel, ILifetimeScope lifetimeScope) : IServiceLocatorScope
   {
      readonly AutofacDependencyInjectionContainer _container = container;
      readonly IScopeServiceLocator _scopedKernel = scopedKernel;
      readonly ILifetimeScope _lifetimeScope = lifetimeScope;

      public TComponent Resolve<TComponent>() where TComponent : class => _scopedKernel.Resolve<TComponent>();

      public object Resolve(Type serviceType)
      {
         if(_container.TryCreateTransientInstance(serviceType, _scopedKernel, out var transientInstance))
            return transientInstance;
         return _lifetimeScope.Resolve(serviceType);
      }

      public void Dispose() => _lifetimeScope.Dispose();
   }
}
