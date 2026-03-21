using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Threading;
using DryIoc;

namespace Compze.DependencyInjection.DryIoc;

public sealed class DryIocContainerBuilder(IComponentRegistrar? registrar = null) : ContainerBuilder(registrar), IDryIocBuilderInternals
{
   readonly IContainer _container = new Container(rules => rules
      .WithTrackingDisposableTransients()
      .With(FactoryMethod.ConstructorWithResolvableArguments));
   readonly RunOnce _registerScopedKernel = new();

   public override DryIocContainer Build() => (DryIocContainer)base.Build();

   protected override void RegisterInContainer(ComponentRegistration[] registrations)
   {
      _registerScopedKernel.RunIfFirstCall(() =>
      {
         _container.RegisterDelegate<DisposableTracker>(
            _ => new DisposableTracker(),
            Reuse.ScopedOrSingleton,
            ifAlreadyRegistered: IfAlreadyRegistered.Replace);
         _container.RegisterDelegate<ScopeResolver>(
            resolver => new ScopeResolver(serviceType => resolver.Resolve(serviceType)),
            Reuse.Scoped,
            ifAlreadyRegistered: IfAlreadyRegistered.Replace);
         _container.RegisterDelegate<IScopeResolver>(
            resolver => resolver.Resolve<ScopeResolver>(),
            Reuse.Scoped,
            ifAlreadyRegistered: IfAlreadyRegistered.Replace);
      });

      foreach(var registration in registrations)
      {
         var serviceTypes = registration.ServiceTypes.ToArray();
         var firstServiceType = serviceTypes.First();

         switch(registration.Lifestyle)
         {
            case Lifestyle.Singleton:
               if(registration.InstantiationSpec.SingletonInstance is {} instance)
               {
                  serviceTypes.ForEach(serviceType => _container.RegisterInstance(serviceType, instance,
                     ifAlreadyRegistered: IfAlreadyRegistered.Throw,
                     setup: Setup.With(preventDisposal: true)));
               } else
               {
                  _container.RegisterDelegate(firstServiceType,
                     resolver => registration.InstantiationSpec.RunFactoryMethod(new ServiceResolver(resolver.Resolve)),
                     Reuse.Singleton,
                     ifAlreadyRegistered: IfAlreadyRegistered.Throw);

                  RegisterForwardingTypes(serviceTypes, firstServiceType, Reuse.Singleton);
               }

               break;
            case Lifestyle.Scoped:
               _container.RegisterDelegate(firstServiceType,
                  resolver => registration.InstantiationSpec.RunFactoryMethod(resolver.Resolve<ScopeResolver>()),
                  Reuse.Scoped,
                  ifAlreadyRegistered: IfAlreadyRegistered.Throw);

               RegisterForwardingTypes(serviceTypes, firstServiceType, Reuse.Scoped);
               break;
            case Lifestyle.TrackedTransient:
               _container.RegisterDelegate(firstServiceType,
                  resolver =>
                  {
                     var instance = registration.InstantiationSpec.RunFactoryMethod(new ServiceResolver(resolver.Resolve));
                     resolver.Resolve<DisposableTracker>().Track(instance);
                     return instance;
                  },
                  Reuse.Transient,
                  ifAlreadyRegistered: IfAlreadyRegistered.Throw);

               RegisterForwardingTypes(serviceTypes, firstServiceType, Reuse.Transient);
               break;
            default:
               throw new ArgumentOutOfRangeException(nameof(registration.Lifestyle), registration.Lifestyle, $"Unsupported lifestyle: {registration.Lifestyle}");
         }
      }
   }

   void RegisterForwardingTypes(Type[] serviceTypes, Type firstServiceType, IReuse reuse)
   {
      foreach(var serviceType in serviceTypes.Skip(1))
      {
         _container.RegisterDelegate(serviceType,
            resolver => resolver.Resolve(firstServiceType),
            reuse,
            ifAlreadyRegistered: IfAlreadyRegistered.Throw);
      }
   }

   protected override DependencyInjectionContainer BuildInternal()
   {
      // Auto-register intrinsic container types via closures that will be filled after build
      DryIocContainer builtContainer = null!;
      // ReSharper disable AccessToModifiedClosure
      _container.RegisterDelegate<IDependencyInjectionContainer>(_ => builtContainer!, Reuse.Singleton, ifAlreadyRegistered: IfAlreadyRegistered.Replace);
      _container.RegisterDelegate<IRootResolver>(_ => builtContainer!, Reuse.Singleton, ifAlreadyRegistered: IfAlreadyRegistered.Replace);
      _container.RegisterDelegate<IScopeFactory>(_ => builtContainer!, Reuse.Singleton, ifAlreadyRegistered: IfAlreadyRegistered.Replace);
      _container.RegisterDelegate(_ => builtContainer!, Reuse.Singleton, ifAlreadyRegistered: IfAlreadyRegistered.Replace);
      // ReSharper restore AccessToModifiedClosure

      builtContainer = new DryIocContainer(_container, RegisteredComponents(), Registrar);
      return builtContainer;
   }
   IContainer IDryIocBuilderInternals.Container => _container;
}
