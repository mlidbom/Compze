using Autofac;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.LinqCE;

namespace Compze.DependencyInjection.Autofac;

public sealed class AutofacContainerBuilder(IComponentRegistrar? registrar = null) : ContainerBuilder(registrar), IAutofacBuilderInternals
{
   readonly global::Autofac.ContainerBuilder _containerBuilder = new();

   public override AutofacContainer Build(ContainerOptions? options = null) => (AutofacContainer)base.Build(options);

   protected override void RegisterInContainer(ComponentRegistration[] registrations)
   {
      _containerBuilder.Register(componentContext =>
      {
         var scope = componentContext.Resolve<ILifetimeScope>();
         return new ScopeResolver(scope.Resolve);
      }).InstancePerLifetimeScope();
      _containerBuilder.Register(componentContext => (IScopeResolver)componentContext.Resolve<ScopeResolver>())
                       .As<IScopeResolver>()
                       .InstancePerLifetimeScope();

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
                  _containerBuilder.Register(componentContext => registration.InstantiationSpec.RunFactoryMethod(LifetimeStableResolver(componentContext)))
                                   .As(serviceTypes)
                                   .SingleInstance();
               }

               break;
            case Lifestyle.Scoped:
               _containerBuilder.Register(componentContext =>
                                {
                                   if(!Options.AllowScopedResolutionFromRoot)
                                   {
                                      var currentScope = componentContext.Resolve<ILifetimeScope>();
                                      if(currentScope.Tag is "root")
                                         throw new InvalidOperationException(
                                            $"Cannot resolve scoped service '{serviceTypes[0].FullName}' from the root container. "
                                            + "Scoped services must be resolved within a scope. Call BeginScope() first, or set "
                                            + "ContainerOptions.AllowScopedResolutionFromRoot to opt into the broken-by-design behavior.");
                                   }

                                   return registration.InstantiationSpec.RunFactoryMethod(componentContext.Resolve<ScopeResolver>());
                                })
                                .As(serviceTypes)
                                .InstancePerLifetimeScope();
               break;
            case Lifestyle.TrackedTransient:
               _containerBuilder.Register(componentContext => registration.InstantiationSpec.RunFactoryMethod(LifetimeStableResolver(componentContext)))
                                .As(serviceTypes)
                                .InstancePerDependency();
               break;
            default:
               throw new ArgumentOutOfRangeException(nameof(registration.Lifestyle), registration.Lifestyle, $"Unsupported lifestyle: {registration.Lifestyle}");
         }
      }
   }

   // Wraps the Resolve of the owning ILifetimeScope, NOT of the IComponentContext. The context is only valid during the
   // current resolve operation, but a component may hold onto its IServiceResolver and resolve through it later — e.g. an
   // IServiceResolver<T> taken to break a circular dependency. The lifetime scope stays valid for the component's lifetime.
   static ServiceResolver LifetimeStableResolver(IComponentContext componentContext) =>
      new(componentContext.Resolve<ILifetimeScope>().Resolve);

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
