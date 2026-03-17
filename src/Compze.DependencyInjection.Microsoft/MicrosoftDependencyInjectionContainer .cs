using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Threading;
using Microsoft.Extensions.DependencyInjection;
using IServiceScope = Compze.DependencyInjection.Abstractions.IServiceScope;

namespace Compze.DependencyInjection.Microsoft;

public sealed class MicrosoftDependencyInjectionContainer(IComponentRegistrar? register = null) : DependencyInjectionContainer(register), IServiceLocator, IMicrosoftContainerInternals
{
   readonly IServiceCollection _services = new ServiceCollection();
   ServiceProvider? _serviceProvider;
   bool _isDisposed;

   protected override DependencyInjectionContainer CreateEmptyClone() =>
      new MicrosoftDependencyInjectionContainer(Register().Clone());

   readonly RunOnce _registerScopedKernel = new();

   protected override IDependencyInjectionContainer RegisterInContainer(ComponentRegistration[] registrations)
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

      return this;
   }

   public override IServiceLocator ServiceLocator
   {
      get
      {
         if(_serviceProvider == null)
         {
            AssertLifeStyleCombinationsAreValid();
            _serviceProvider = _services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
         }

         return this;
      }
   }

   public object Resolve(Type serviceType)
   {
      Contract.State.NotDisposed(_isDisposed, this);
      return _serviceProvider._assert().NotNull().GetRequiredService(serviceType);
   }

   public IServiceScope BeginScope()
   {
      Contract.State.NotDisposed(_isDisposed, this);

      var scope = _serviceProvider._assert().NotNull().CreateAsyncScope();
      var scopeResolver = scope.ServiceProvider.GetRequiredService<ScopeResolverWrapper>();

      return new ServiceLocatorScope(scopeResolver, () => scope.DisposeAsync().AsTask().GetAwaiter().GetResult());
   }

   IServiceCollection IMicrosoftContainerInternals.ServiceCollection => _services;
   IServiceProvider IMicrosoftContainerInternals.ServiceProvider => _serviceProvider._assert().NotNull();

   public override void Dispose()
   {
      if(!_isDisposed)
      {
         _isDisposed = true;
         if(_serviceProvider != null)
         {
            _serviceProvider.DisposeAsync().AsTask().GetAwaiter().GetResult();
         }

         _serviceProvider = null;
      }
   }

   public override async ValueTask DisposeAsync()
   {
      if(!_isDisposed)
      {
         _isDisposed = true;
         if(_serviceProvider != null)
         {
            await _serviceProvider.DisposeAsync().caf();
         }

         _serviceProvider = null;
      }
   }

   sealed class ServiceLocatorScope(IScopeResolver scopeResolver, Action onDispose) : IServiceScope
   {
      public IScopeResolver Resolver { get; } = scopeResolver;

      public void Dispose() => onDispose();
   }
}
