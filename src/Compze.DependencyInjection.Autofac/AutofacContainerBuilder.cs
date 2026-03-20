using Autofac;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Threading;

namespace Compze.DependencyInjection.Autofac;

public sealed class AutofacContainerBuilder(IComponentRegistrar? registrar = null) : ContainerBuilder(registrar), IAutofacBuilderInternals
{
   readonly global::Autofac.ContainerBuilder _containerBuilder = new();
   readonly RunOnce _registerScopedKernel = new();

   public override AutofacContainer Build() => (AutofacContainer)base.Build();

   protected override void RegisterInContainer(ComponentRegistration[] registrations)
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
   }

   protected override DependencyInjectionContainer BuildInternal()
   {
      // Auto-register intrinsic container types via closures that will be filled after build
      AutofacContainer builtContainer = null!;
      // ReSharper disable AccessToModifiedClosure
      _containerBuilder.Register(_ => (IDependencyInjectionContainer)builtContainer).As<IDependencyInjectionContainer>().SingleInstance().ExternallyOwned();
      _containerBuilder.Register(_ => (IRootResolver)builtContainer).As<IRootResolver>().SingleInstance().ExternallyOwned();
      _containerBuilder.Register(_ => (IScopeFactory)builtContainer).As<IScopeFactory>().SingleInstance().ExternallyOwned();
      _containerBuilder.Register(_ => builtContainer).SingleInstance().ExternallyOwned();
      // ReSharper restore AccessToModifiedClosure
      var container = _containerBuilder.Build();
      builtContainer = new AutofacContainer(container, RegisteredComponents(), Registrar);
      return builtContainer;
   }
   global::Autofac.ContainerBuilder IAutofacBuilderInternals.ContainerBuilder => _containerBuilder;
}
