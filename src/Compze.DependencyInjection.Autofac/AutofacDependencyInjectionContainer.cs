using Autofac;
using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Threading;

namespace Compze.DependencyInjection.Autofac;

public sealed class AutofacDependencyInjectionContainer(IComponentRegistrar? register = null) : DependencyInjectionContainer(register), IServiceLocator, IAutofacContainerInternals
{
   readonly ContainerBuilder _containerBuilder = new();
   IContainer? _container;

   readonly RunOnce _registerScopedKernel = new();

   protected override IDependencyInjectionContainer RegisterInContainer(ComponentRegistration[] registrations)
   {
      _registerScopedKernel.RunIfFirstCall(() =>
      {
         _containerBuilder.Register(componentContext =>
         {
            var scope = componentContext.Resolve<ILifetimeScope>();
            return new ScopeResolverWrapper(scope.Resolve);
         }).InstancePerLifetimeScope();
         _containerBuilder.Register(componentContext => (IScopeResolver)componentContext.Resolve<ScopeResolverWrapper>())
                          .As<IScopeResolver>()
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
                  _containerBuilder.Register(componentContext => registration.InstantiationSpec.RunFactoryMethod(new ServiceResolverWrapper(componentContext.Resolve)))
                                   .As(serviceTypes)
                                   .SingleInstance();
               }

               break;
            case Lifestyle.Scoped:
               _containerBuilder.Register(componentContext => registration.InstantiationSpec.RunFactoryMethod(componentContext.Resolve<ScopeResolverWrapper>()))
                                .As(serviceTypes)
                                .InstancePerLifetimeScope();
               break;
            case Lifestyle.TrackedTransient:
               _containerBuilder.Register(componentContext => registration.InstantiationSpec.RunFactoryMethod(new ServiceResolverWrapper(componentContext.Resolve)))
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

   protected override DependencyInjectionContainer CreateEmptyClone() =>
      new AutofacDependencyInjectionContainer(Register().Clone());

   protected override IReadOnlyList<Type> ContainerFacadeServiceTypes { get; } =
      [typeof(IDependencyInjectionContainer), typeof(IServiceLocator), typeof(AutofacDependencyInjectionContainer)];

   IContainer IAutofacContainerInternals.Container => _container._assert().NotNull();
   ContainerBuilder IAutofacContainerInternals.ContainerBuilder => _containerBuilder;

   public object Resolve(Type serviceType) =>
      _container._assert().NotNull().Resolve(serviceType);

   public IServiceScope BeginScope()
   {
      var lifetimeScope = _container._assert().NotNull().BeginLifetimeScope();
      var scopeResolver = lifetimeScope.Resolve<ScopeResolverWrapper>();
      return new ServiceLocatorScope(scopeResolver, lifetimeScope);
   }

   public override void Dispose() => _container?.Dispose();

   public override async ValueTask DisposeAsync()
   {
      if(_container != null)
         await _container.DisposeAsync().caf();
   }

   sealed class ServiceLocatorScope(IScopeResolver scopeResolver, ILifetimeScope lifetimeScope) : IServiceScope
   {
      readonly ILifetimeScope _lifetimeScope = lifetimeScope;
      public IScopeResolver Resolver { get; } = scopeResolver;

      public void Dispose() => _lifetimeScope.Dispose();
   }
}
