using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.LinqCE;
using Microsoft.Extensions.DependencyInjection;
using Compze.DependencyInjection.Microsoft._private;

namespace Compze.DependencyInjection.Microsoft;

public sealed class MicrosoftContainerBuilder(IComponentRegistrar? registrar = null) : ContainerBuilder(registrar), IMicrosoftBuilderInternals
{
   readonly IServiceCollection _services = new ServiceCollection();

   public override MicrosoftContainer Build(ContainerOptions? options = null) => (MicrosoftContainer)base.Build(options);

   protected override void RegisterInContainer(ComponentRegistration[] registrations)
   {
      // Guards a component-factory-injected or scope-level IServiceResolver against resolving through the wrong one of
      // Resolve/ResolveSet — see DependencyInjectionContainer.Resolve/ResolveSet for the equivalent root-level guard.
      var componentSetServiceTypes = registrations.Where(it => it.IsComponentSetMember).SelectMany(it => it.ServiceTypes).ToHashSet();
      var singularServiceTypes = registrations.Where(it => !it.IsComponentSetMember).SelectMany(it => it.ServiceTypes).ToHashSet();
      ServiceResolver GuardedServiceResolver(IServiceProvider serviceProvider) =>
         new(serviceType => ComponentSetExclusivityGuard.Resolve(serviceType, componentSetServiceTypes, serviceProvider.GetRequiredService),
             serviceType => ComponentSetExclusivityGuard.ResolveSet(serviceType, singularServiceTypes, resolvedServiceType => serviceProvider.GetServices(resolvedServiceType).Cast<object>()));

      _services.AddScoped<ScopeResolver>(serviceProvider => new ScopeResolver(
         serviceType => ComponentSetExclusivityGuard.Resolve(serviceType, componentSetServiceTypes, serviceProvider.GetRequiredService),
         serviceType => ComponentSetExclusivityGuard.ResolveSet(serviceType, singularServiceTypes, resolvedServiceType => serviceProvider.GetServices(resolvedServiceType).Cast<object>())));
      _services.AddScoped<IScopeResolver>(serviceProvider => serviceProvider.GetRequiredService<ScopeResolver>());

      foreach(var registration in registrations)
      {
         var firstServiceType = registration.ServiceTypes.First();
         var lifetime = registration.Lifestyle.AsServiceLifetime();

         switch(registration.Lifestyle)
         {
            case Lifestyle.Singleton:
               if(registration.InstantiationSpec.SingletonInstance is {} instance)
               {
                  registration.ServiceTypes.ForEach(it => _services.AddSingleton(it, instance));
               } else
               {
                  _services.Add(new ServiceDescriptor(firstServiceType,
                                                      serviceProvider => registration.InstantiationSpec.RunFactoryMethod(GuardedServiceResolver(serviceProvider)),
                                                      lifetime));
               }

               break;
            case Lifestyle.Scoped:
               _services.Add(new ServiceDescriptor(firstServiceType,
                                                   serviceProvider => registration.InstantiationSpec.RunFactoryMethod(serviceProvider.GetRequiredService<ScopeResolver>()),
                                                   lifetime));
               break;
            case Lifestyle.TrackedTransient:
               _services.Add(new ServiceDescriptor(firstServiceType,
                                                   serviceProvider => registration.InstantiationSpec.RunFactoryMethod(GuardedServiceResolver(serviceProvider)),
                                                   lifetime));
               break;
            default:
               throw new ArgumentOutOfRangeException(nameof(registration.Lifestyle), registration.Lifestyle, $"Unsupported lifestyle: {registration.Lifestyle}");
         }

         // Component set members register under exactly one service type (ForSet<TService>() takes a single type), so this loop
         // never runs for them — the forwarding it sets up is only meaningful for a singular multi-service-type registration.
         foreach(var serviceType in registration.ServiceTypes.Skip(1))
         {
            _services.Add(new ServiceDescriptor(serviceType, serviceProvider => serviceProvider.GetService(firstServiceType)!, lifetime));
         }
      }
   }

   protected override DependencyInjectionContainer BuildInternal()
   {
      // Auto-register intrinsic container types via closures that will be filled after build
      MicrosoftContainer? builtContainer = null;
      // ReSharper disable AccessToModifiedClosure
      _services.AddSingleton<IDependencyInjectionContainer>(_ => builtContainer!);
      _services.AddSingleton<IRootResolver>(_ => builtContainer!);
      _services.AddSingleton<IScopeFactory>(_ => builtContainer!);
      _services.AddSingleton(_ => builtContainer!);
      // ReSharper restore AccessToModifiedClosure

      var serviceProvider = _services.BuildServiceProvider(new ServiceProviderOptions
      {
         ValidateScopes = !Options.AllowScopedResolutionFromRoot
      });
      builtContainer = new MicrosoftContainer(serviceProvider, RegisteredComponents(), Registrar);
      return builtContainer;
   }
   IServiceCollection IMicrosoftBuilderInternals.ServiceCollection => _services;
}
