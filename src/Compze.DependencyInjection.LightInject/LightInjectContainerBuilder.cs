using System.Reflection;
using Compze.DependencyInjection.Abstractions;
using LightInject;
using ContainerOptions = Compze.DependencyInjection.Abstractions.ContainerOptions;

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

      _container.Register<ScopeResolver>(
         factory => new ScopeResolver(factory.GetInstance),
         scopedLifetime);
      _container.Register<IScopeResolver>(
         factory => factory.GetInstance<ScopeResolver>(),
         scopedLifetime);

      foreach(var registration in registrations)
      {
         var serviceTypes = registration.ServiceTypes.ToArray();
         var firstServiceType = serviceTypes.First();

         switch(registration.Lifestyle)
         {
            case Lifestyle.Singleton:
               if(registration.InstantiationSpec.SingletonInstance is {} instance)
               {
                  foreach(var serviceType in serviceTypes)
                     _container.RegisterInstance(serviceType, instance);
               } else
               {
                  RegisterFactory(firstServiceType,
                     factory => registration.InstantiationSpec.RunFactoryMethod(new ServiceResolver(factory.GetInstance)),
                     new PerContainerLifetime());

                  RegisterForwardingTypes(serviceTypes, firstServiceType, new PerContainerLifetime());
               }

               break;
            case Lifestyle.Scoped:
               RegisterFactory(firstServiceType,
                  factory => registration.InstantiationSpec.RunFactoryMethod(factory.GetInstance<ScopeResolver>()),
                  scopedLifetime);

               RegisterForwardingTypes(serviceTypes, firstServiceType, scopedLifetime);
               break;
            case Lifestyle.TrackedTransient:
               RegisterFactory(firstServiceType,
                  factory =>
                  {
                     var inst = registration.InstantiationSpec.RunFactoryMethod(new ServiceResolver(factory.GetInstance));
                     CurrentTrackedTransientTracker.Value?.Track(inst);
                     return inst;
                  });

               RegisterForwardingTypes(serviceTypes, firstServiceType);
               break;
            default:
               throw new ArgumentOutOfRangeException(nameof(registration.Lifestyle), registration.Lifestyle, $"Unsupported lifestyle: {registration.Lifestyle}");
         }
      }
   }

   void RegisterFactory(Type serviceType, Func<IServiceFactory, object> factory, ILifetime? lifetime = null) =>
      _container.Register(new ServiceRegistration
      {
         ServiceType = serviceType,
         FactoryExpression = CreateTypedFactory(serviceType, factory),
         Lifetime = lifetime
      });

   void RegisterForwardingTypes(Type[] serviceTypes, Type firstServiceType, ILifetime? lifetime = null)
   {
      foreach(var serviceType in serviceTypes.Skip(1))
         RegisterFactory(serviceType, factory => factory.GetInstance(firstServiceType), lifetime);
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
