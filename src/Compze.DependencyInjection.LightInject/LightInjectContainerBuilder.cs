using System.Reflection;
using Compze.DependencyInjection.Abstractions;
using LightInject;
using ContainerOptions = Compze.DependencyInjection.Abstractions.ContainerOptions;
using Compze.DependencyInjection.LightInject.Private;

namespace Compze.DependencyInjection.LightInject;

#pragma warning disable CA1001 // The underlying container holds no live resources until Build() is called (registrations live on the base class until then); on Build() ownership transfers to the LightInjectContainer wrapper which handles disposal.
public sealed class LightInjectContainerBuilder(IComponentRegistrar? registrar = null) : ContainerBuilder(registrar), ILightInjectBuilderInternals
#pragma warning restore CA1001
{
   internal static readonly AsyncLocal<DisposableTracker?> CurrentTrackedTransientTracker = new();

   readonly ServiceContainer _container = new(options =>
   {
      options.EnablePropertyInjection = false;
      options.EnableCurrentScope = false;
      options.EnableOptionalArguments = true;
      options.EnableMicrosoftCompatibility = true;
   });

   public override LightInjectContainer Build(ContainerOptions? options = null) => (LightInjectContainer)base.Build(options);

   protected override void RegisterInContainer(ComponentRegistration[] registrations)
   {
      var scopedLifetime = Options.AllowScopedResolutionFromRoot
         ? (ILifetime)new PerContainerLifetime()
         : new PerScopeLifetime();

      // Guards a component-factory-injected or scope-level IServiceResolver against resolving through the wrong one of
      // Resolve/ResolveSet — see DependencyInjectionContainer.Resolve/ResolveSet for the equivalent root-level guard.
      var componentSetServiceTypes = registrations.Where(it => it.IsComponentSetMember).SelectMany(it => it.ServiceTypes).ToHashSet();
      var singularServiceTypes = registrations.Where(it => !it.IsComponentSetMember).SelectMany(it => it.ServiceTypes).ToHashSet();
      var memberNamesByServiceType = LightInjectComponentSetNaming.ComputeMemberNamesByServiceType(registrations);

      _container.Register<ScopeResolver>(
         factory => new ScopeResolver(
            serviceType => ComponentSetExclusivityGuard.Resolve(serviceType, componentSetServiceTypes, factory.GetInstance),
            serviceType => ComponentSetExclusivityGuard.ResolveSet(serviceType, singularServiceTypes, resolvedServiceType => ResolveComponentSetMembers(factory, memberNamesByServiceType, resolvedServiceType))),
         scopedLifetime);
      _container.Register<IScopeResolver>(
         factory => factory.GetInstance<ScopeResolver>(),
         scopedLifetime);

      for(var index = 0; index < registrations.Length; index++)
      {
         var registration = registrations[index];
         var serviceTypes = registration.ServiceTypes.ToArray();
         var firstServiceType = serviceTypes.First();
         var serviceName = registration.IsComponentSetMember ? LightInjectComponentSetNaming.NameFor(index) : string.Empty;

         switch(registration.Lifestyle)
         {
            case Lifestyle.Singleton:
               if(registration.InstantiationSpec.SingletonInstance is {} instance)
               {
                  foreach(var serviceType in serviceTypes)
                     _container.RegisterInstance(serviceType, instance, serviceName);
               } else
               {
                  RegisterFactory(firstServiceType,
                     factory => registration.InstantiationSpec.RunFactoryMethod(GuardedServiceResolver(factory, componentSetServiceTypes, singularServiceTypes, memberNamesByServiceType)),
                     serviceName,
                     new PerContainerLifetime());

                  RegisterForwardingTypes(serviceTypes, firstServiceType, new PerContainerLifetime());
               }

               break;
            case Lifestyle.Scoped:
               RegisterFactory(firstServiceType,
                  factory => registration.InstantiationSpec.RunFactoryMethod(factory.GetInstance<ScopeResolver>()),
                  serviceName,
                  scopedLifetime);

               RegisterForwardingTypes(serviceTypes, firstServiceType, scopedLifetime);
               break;
            case Lifestyle.TrackedTransient:
               RegisterFactory(firstServiceType,
                  factory =>
                  {
                     var inst = registration.InstantiationSpec.RunFactoryMethod(GuardedServiceResolver(factory, componentSetServiceTypes, singularServiceTypes, memberNamesByServiceType));
                     CurrentTrackedTransientTracker.Value?.Track(inst);
                     return inst;
                  },
                  serviceName);

               RegisterForwardingTypes(serviceTypes, firstServiceType);
               break;
            default:
               throw new ArgumentOutOfRangeException(nameof(registration.Lifestyle), registration.Lifestyle, $"Unsupported lifestyle: {registration.Lifestyle}");
         }
      }
   }

   static ServiceResolver GuardedServiceResolver(IServiceFactory factory,
                                                 IReadOnlySet<Type> componentSetServiceTypes,
                                                 IReadOnlySet<Type> singularServiceTypes,
                                                 IReadOnlyDictionary<Type, IReadOnlyList<string>> memberNamesByServiceType) =>
      new(serviceType => ComponentSetExclusivityGuard.Resolve(serviceType, componentSetServiceTypes, factory.GetInstance),
          serviceType => ComponentSetExclusivityGuard.ResolveSet(serviceType, singularServiceTypes, resolvedServiceType => ResolveComponentSetMembers(factory, memberNamesByServiceType, resolvedServiceType)));

   internal static IEnumerable<object> ResolveComponentSetMembers(IServiceFactory factory, IReadOnlyDictionary<Type, IReadOnlyList<string>> memberNamesByServiceType, Type serviceType) =>
      memberNamesByServiceType.TryGetValue(serviceType, out var names) ? names.Select(name => factory.GetInstance(serviceType, name)) : [];

   // Each registration — singular or component set member alike — gets its own LightInject service name (empty for singular,
   // matching pre-existing behavior; an index-derived name for a set member, resolved back via ResolveComponentSetMembers
   // rather than GetAllInstances — see LightInjectComponentSetNaming for why).
   void RegisterFactory(Type serviceType, Func<IServiceFactory, object> factory, string serviceName = "", ILifetime? lifetime = null) =>
      _container.Register(new ServiceRegistration
      {
         ServiceType = serviceType,
         ServiceName = serviceName,
         FactoryExpression = CreateTypedFactory(serviceType, factory),
         Lifetime = lifetime
      });

   void RegisterForwardingTypes(Type[] serviceTypes, Type firstServiceType, ILifetime? lifetime = null)
   {
      // Component set members register under exactly one service type (ForSet<TService>() takes a single type), so this loop
      // never runs for them — the forwarding it sets up is only meaningful for a singular multi-service-type registration.
      foreach(var serviceType in serviceTypes.Skip(1))
         RegisterFactory(serviceType, factory => factory.GetInstance(firstServiceType), lifetime: lifetime);
   }

   static readonly MethodInfo CreateTypedFactoryHelperMethod =
      typeof(LightInjectContainerBuilder).GetMethod(nameof(CreateTypedFactoryHelper), BindingFlags.NonPublic | BindingFlags.Static)!;

   static Delegate CreateTypedFactory(Type serviceType, Func<IServiceFactory, object> factoryMethod) =>
      (Delegate)CreateTypedFactoryHelperMethod.MakeGenericMethod(serviceType).Invoke(null, [factoryMethod])!;

   static Func<IServiceFactory, T> CreateTypedFactoryHelper<T>(Func<IServiceFactory, object> factory) =>
      f => (T)factory(f);

   protected override DependencyInjectionContainer BuildInternal()
   {
      LightInjectContainer builtContainer = null!;
      // ReSharper disable AccessToModifiedClosure
      _container.Register<IDependencyInjectionContainer>(_ => builtContainer, new PerContainerLifetime());
      _container.Register<IRootResolver>(_ => builtContainer, new PerContainerLifetime());
      _container.Register<IScopeFactory>(_ => builtContainer, new PerContainerLifetime());
      _container.Register(_ => builtContainer, new PerContainerLifetime());
      // ReSharper restore AccessToModifiedClosure

      builtContainer = new LightInjectContainer(_container, RegisteredComponents(), Registrar);
      return builtContainer;
   }

   IServiceContainer ILightInjectBuilderInternals.ServiceContainer => _container;
}
