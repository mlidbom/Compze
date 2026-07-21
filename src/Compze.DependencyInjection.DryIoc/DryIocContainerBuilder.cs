using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.LinqCE;
using DryIoc;
using Compze.DependencyInjection.DryIoc.Private;

namespace Compze.DependencyInjection.DryIoc;

#pragma warning disable CA1001 // The underlying container holds no live resources until Build() is called (registrations live on the base class until then); on Build() ownership transfers to the DryIocContainer wrapper which handles disposal.
public sealed class DryIocContainerBuilder(IComponentRegistrar? registrar = null) : ContainerBuilder(registrar), IDryIocBuilderInternals
#pragma warning restore CA1001
{
   readonly IContainer _container = new Container(rules => rules
      .WithTrackingDisposableTransients()
      .With(FactoryMethod.ConstructorWithResolvableArguments));

   public override DryIocContainer Build(ContainerOptions? options = null) => (DryIocContainer)base.Build(options);

   protected override void RegisterInContainer(ComponentRegistration[] registrations)
   {
      var scopedReuse = Options.AllowScopedResolutionFromRoot ? Reuse.ScopedOrSingleton : Reuse.Scoped;

      // Guards a component-factory-injected or scope-level IServiceResolver against resolving through the wrong one of
      // Resolve/ResolveSet — see DependencyInjectionContainer.Resolve/ResolveSet for the equivalent root-level guard.
      var componentSetServiceTypes = registrations.Where(it => it.IsComponentSetMember).SelectMany(it => it.ServiceTypes).ToHashSet();
      var singularServiceTypes = registrations.Where(it => !it.IsComponentSetMember).SelectMany(it => it.ServiceTypes).ToHashSet();

      _container.RegisterDelegate<DisposableTracker>(
         _ => new DisposableTracker(),
         Reuse.ScopedOrSingleton,
         ifAlreadyRegistered: IfAlreadyRegistered.Replace);
      _container.RegisterDelegate<ScopeResolver>(
         resolver => new ScopeResolver(
            serviceType => ComponentSetExclusivityGuard.Resolve(serviceType, componentSetServiceTypes, resolver.Resolve),
            serviceType => ComponentSetExclusivityGuard.ResolveSet(serviceType, singularServiceTypes, resolvedServiceType => ResolveAll(resolver, resolvedServiceType))),
         scopedReuse,
         ifAlreadyRegistered: IfAlreadyRegistered.Replace);
      _container.RegisterDelegate<IScopeResolver>(
         resolver => resolver.Resolve<ScopeResolver>(),
         scopedReuse,
         ifAlreadyRegistered: IfAlreadyRegistered.Replace);

      foreach(var registration in registrations)
      {
         var serviceTypes = registration.ServiceTypes.ToArray();
         var firstServiceType = serviceTypes.First();
         var ifAlreadyRegistered = registration.IsComponentSetMember ? IfAlreadyRegistered.AppendNotKeyed : IfAlreadyRegistered.Throw;

         switch(registration.Lifestyle)
         {
            case Lifestyle.Singleton:
               if(registration.InstantiationSpec.SingletonInstance is {} instance)
               {
                  serviceTypes.ForEach(serviceType => _container.RegisterInstance(serviceType, instance,
                     ifAlreadyRegistered: ifAlreadyRegistered,
                     setup: Setup.With(preventDisposal: true)));
               } else
               {
                  _container.RegisterDelegate(firstServiceType,
                     resolver => registration.InstantiationSpec.RunFactoryMethod(GuardedServiceResolver(resolver, componentSetServiceTypes, singularServiceTypes)),
                     Reuse.Singleton,
                     ifAlreadyRegistered: ifAlreadyRegistered);

                  RegisterForwardingTypes(serviceTypes, firstServiceType, Reuse.Singleton);
               }

               break;
            case Lifestyle.Scoped:
               _container.RegisterDelegate(firstServiceType,
                  resolver => registration.InstantiationSpec.RunFactoryMethod(resolver.Resolve<ScopeResolver>()),
                  scopedReuse,
                  ifAlreadyRegistered: ifAlreadyRegistered);

               RegisterForwardingTypes(serviceTypes, firstServiceType, scopedReuse);
               break;
            case Lifestyle.TrackedTransient:
               _container.RegisterDelegate(firstServiceType,
                  resolver =>
                  {
                     var trackedInstance = registration.InstantiationSpec.RunFactoryMethod(GuardedServiceResolver(resolver, componentSetServiceTypes, singularServiceTypes));
                     resolver.Resolve<DisposableTracker>().Track(trackedInstance);
                     return trackedInstance;
                  },
                  Reuse.Transient,
                  ifAlreadyRegistered: ifAlreadyRegistered);

               RegisterForwardingTypes(serviceTypes, firstServiceType, Reuse.Transient);
               break;
            default:
               throw new ArgumentOutOfRangeException(nameof(registration.Lifestyle), registration.Lifestyle, $"Unsupported lifestyle: {registration.Lifestyle}");
         }
      }
   }

   static ServiceResolver GuardedServiceResolver(IResolverContext resolver, IReadOnlySet<Type> componentSetServiceTypes, IReadOnlySet<Type> singularServiceTypes) =>
      new(serviceType => ComponentSetExclusivityGuard.Resolve(serviceType, componentSetServiceTypes, resolver.Resolve),
          serviceType => ComponentSetExclusivityGuard.ResolveSet(serviceType, singularServiceTypes, resolvedServiceType => ResolveAll(resolver, resolvedServiceType)));

   static IEnumerable<object> ResolveAll(IResolverContext resolver, Type serviceType) =>
      (IEnumerable<object>)resolver.Resolve(typeof(IEnumerable<>).MakeGenericType(serviceType));

   void RegisterForwardingTypes(Type[] serviceTypes, Type firstServiceType, IReuse reuse)
   {
      // Component set members register under exactly one service type (ForSet<TService>() takes a single type), so this loop
      // never runs for them — the forwarding it sets up is only meaningful for a singular multi-service-type registration.
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
      _container.RegisterDelegate<IDependencyInjectionContainer>(_ => builtContainer, Reuse.Singleton, ifAlreadyRegistered: IfAlreadyRegistered.Replace);
      _container.RegisterDelegate<IRootResolver>(_ => builtContainer, Reuse.Singleton, ifAlreadyRegistered: IfAlreadyRegistered.Replace);
      _container.RegisterDelegate<IScopeFactory>(_ => builtContainer, Reuse.Singleton, ifAlreadyRegistered: IfAlreadyRegistered.Replace);
      _container.RegisterDelegate(_ => builtContainer, Reuse.Singleton, ifAlreadyRegistered: IfAlreadyRegistered.Replace);
      // ReSharper restore AccessToModifiedClosure

      builtContainer = new DryIocContainer(_container, RegisteredComponents(), Registrar);
      return builtContainer;
   }
   IContainer IDryIocBuilderInternals.Container => _container;
}
