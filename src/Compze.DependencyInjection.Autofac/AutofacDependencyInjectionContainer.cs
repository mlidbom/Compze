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
         var serviceTypes = registration.ServiceTypes.ToArray();

         switch(registration.Lifestyle)
         {
            case Lifestyle.Singleton:
               if(registration.InstantiationSpec.SingletonInstance is {} instance)
               {
                  serviceTypes.ForEach(serviceType => _containerBuilder.RegisterInstance(instance)
                                                                       .As(serviceType)
                                                                       .ExternallyOwned());
               } else
               {
                  _containerBuilder.Register(_ => registration.InstantiationSpec.RunFactoryMethod(this))
                                   .As(serviceTypes)
                                   .SingleInstance();
               }

               break;
            case Lifestyle.Scoped:
               _containerBuilder.Register(ctx => registration.InstantiationSpec.RunFactoryMethod(ctx.Resolve<ScopedKernel>()))
                                .As(serviceTypes)
                                .InstancePerLifetimeScope();
               break;
            case Lifestyle.TrackedTransient:
               _containerBuilder.Register(_ => registration.InstantiationSpec.RunFactoryMethod(this))
                                .As(serviceTypes)
                                .InstancePerDependency();
               break;
            default:
               throw new ArgumentOutOfRangeException(nameof(registration.Lifestyle), registration.Lifestyle, $"Unsupported lifestyle: {registration.Lifestyle}");
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

   IContainer IAutofacContainerInternals.Container => _container._assert().NotNull();
   ContainerBuilder IAutofacContainerInternals.ContainerBuilder => _containerBuilder;

   public TComponent Resolve<TComponent>() where TComponent : class =>
      _container._assert().NotNull().Resolve<TComponent>();

   public object Resolve(Type serviceType) =>
      _container._assert().NotNull().Resolve(serviceType);

   TComponent IServiceLocatorKernel.Resolve<TComponent>() =>
      _container._assert().NotNull().Resolve<TComponent>();

   IServiceLocatorScope IServiceLocator.BeginScope()
   {
      var lifetimeScope = _container._assert().NotNull().BeginLifetimeScope();
      var scopedKernel = lifetimeScope.Resolve<ScopedKernel>();
      return new ServiceLocatorScope(scopedKernel, lifetimeScope);
   }

   public override void Dispose() => _container?.Dispose();

   public override async ValueTask DisposeAsync()
   {
      if(_container != null)
         await _container.DisposeAsync().caf();
   }

   sealed class ServiceLocatorScope(IScopeServiceLocator scopedKernel, ILifetimeScope lifetimeScope) : IServiceLocatorScope
   {
      readonly IScopeServiceLocator _scopedKernel = scopedKernel;
      readonly ILifetimeScope _lifetimeScope = lifetimeScope;

      public TComponent Resolve<TComponent>() where TComponent : class => _scopedKernel.Resolve<TComponent>();

      public object Resolve(Type serviceType) => _lifetimeScope.Resolve(serviceType);

      public void Dispose() => _lifetimeScope.Dispose();
   }
}
