using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Compze.DependencyInjection.Microsoft;

public sealed class MicrosoftContainerBuilder(IComponentRegistrar? registrar = null) : ContainerBuilderBase(registrar), IMicrosoftBuilderInternals
{
   readonly IServiceCollection _services = new ServiceCollection();
   readonly RunOnce _registerScopedKernel = new();

   protected override void RegisterInContainer(ComponentRegistration[] registrations)
   {
      _registerScopedKernel.RunIfFirstCall(() =>
      {
         _services.AddScoped<ScopeResolverWrapper>(serviceProvider => new ScopeResolverWrapper(serviceProvider.GetRequiredService));
         _services.AddScoped<IScopeResolver>(serviceProvider => serviceProvider.GetRequiredService<ScopeResolverWrapper>());
      });

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
                                                      serviceProvider => registration.InstantiationSpec.RunFactoryMethod(new ServiceResolverWrapper(serviceProvider.GetRequiredService)),
                                                      lifetime));
               }

               break;
            case Lifestyle.Scoped:
               _services.Add(new ServiceDescriptor(firstServiceType,
                                                   serviceProvider => registration.InstantiationSpec.RunFactoryMethod(serviceProvider.GetRequiredService<ScopeResolverWrapper>()),
                                                   lifetime));
               break;
            case Lifestyle.TrackedTransient:
               _services.Add(new ServiceDescriptor(firstServiceType,
                                                   serviceProvider => registration.InstantiationSpec.RunFactoryMethod(new ServiceResolverWrapper(serviceProvider.GetRequiredService)),
                                                   lifetime));
               break;
            default:
               throw new ArgumentOutOfRangeException(nameof(registration.Lifestyle), registration.Lifestyle, $"Unsupported lifestyle: {registration.Lifestyle}");
         }

         foreach(var serviceType in registration.ServiceTypes.Skip(1))
         {
            _services.Add(new ServiceDescriptor(serviceType, serviceProvider => serviceProvider.GetService(firstServiceType)!, lifetime));
         }
      }
   }

   protected override BuiltContainerBase BuildContainer()
   {
      AssertLifeStyleCombinationsAreValid();

      // Auto-register intrinsic container types via closures that will be filled after build
      MicrosoftBuiltContainer? builtContainer = null;
      _services.AddSingleton<IDependencyInjectionContainer>(_ => builtContainer!);
      _services.AddSingleton<IRootResolver>(_ => builtContainer!);
      _services.AddSingleton<IScopeFactory>(_ => builtContainer!);

      var serviceProvider = _services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
      builtContainer = new MicrosoftBuiltContainer(serviceProvider, RegisteredComponents(), Registrar);
      return builtContainer;
   }

   IServiceCollection IMicrosoftBuilderInternals.ServiceCollection => _services;
}
