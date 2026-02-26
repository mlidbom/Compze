using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compze.Contracts;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;
using Microsoft.Extensions.DependencyInjection;

namespace Compze.Utilities.DependencyInjection.Microsoft;

public sealed class MicrosoftDependencyInjectionContainer : DependencyInjectionContainerBase, IServiceLocator, IServiceLocatorKernel, IMicrosoftContainerInternals
{
   readonly IServiceCollection _services;
   ServiceProvider? _serviceProvider;
   bool _isDisposed;

   readonly AsyncLocal<IServiceScope?> _scopeCache = new();

   public MicrosoftDependencyInjectionContainer(IComponentRegistrar? register = null) : base(register) =>
      _services = new ServiceCollection();

   protected override IDependencyInjectionContainer RegisterInContainer(ComponentRegistration[] registrations)
   {
      foreach(var registration in registrations)
      {
         var lifetime = registration.Lifestyle.AsServiceLifetime();

         if(registration.InstantiationSpec.SingletonInstance is {} instance)
         {
            registration.ServiceTypes.ForEach(it => _services.AddSingleton(it, instance));
         } else
         {
            var firstServiceType = registration.ServiceTypes.First();
            var primaryDescriptor = new ServiceDescriptor(firstServiceType,
                                                          _ => registration.InstantiationSpec.RunFactoryMethod(this),
                                                          lifetime);

            _services.Add(primaryDescriptor);

            foreach(var serviceType in registration.ServiceTypes.Skip(1))
            {
               _services.Add(new ServiceDescriptor(serviceType, sp => sp.GetService(firstServiceType)!, lifetime));
            }
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

   IServiceProvider CurrentProvider()
   {
      Contract.State.NotDisposed(_isDisposed, this);
      return _scopeCache.Value != null ? _scopeCache.Value.ServiceProvider : _serviceProvider._assert().NotNull();
   }

   public TComponent Resolve<TComponent>() where TComponent : class
   {
      Contract.State.NotDisposed(_isDisposed, this);
      return CurrentProvider().GetRequiredService<TComponent>();
   }

   public object Resolve(Type serviceType)
   {
      Contract.State.NotDisposed(_isDisposed, this);
      return CurrentProvider().GetRequiredService(serviceType);
   }

   public TComponent[] ResolveAll<TComponent>() where TComponent : class
   {
      Contract.State.NotDisposed(_isDisposed, this);
      return CurrentProvider().GetServices<TComponent>().ToArray();
   }

   TComponent IServiceLocatorKernel.Resolve<TComponent>()
   {
      Contract.State.NotDisposed(_isDisposed, this);
      return CurrentProvider().GetRequiredService<TComponent>();
   }

   IDisposable IServiceLocator.BeginScope()
   {
      Contract.State.NotDisposed(_isDisposed, this)
            .Assert(_scopeCache.Value == null, () => "Scope already exists. Nested scopes are not supported.");

      _scopeCache.Value = CurrentProvider().CreateAsyncScope();

      return new Disposable(() =>
      {
         Contract.State.Assert(_scopeCache.Value != null, () => "Attempt to dispose scope from a context that is not within the scope.");
         _scopeCache.Value.Dispose();
         _scopeCache.Value = null;
      });
   }

   IServiceCollection IMicrosoftContainerInternals.ServiceCollection => _services;
   IServiceProvider IMicrosoftContainerInternals.ServiceProvider => _serviceProvider._assert().NotNull();

   public override void Dispose()
   {
      if(!_isDisposed)
      {
         Contract.State.Assert(_scopeCache.Value == null, () => "Scopes must be disposed before the container");
         _isDisposed = true;
         _serviceProvider?.Dispose();
         _serviceProvider = null;
      }
   }

   public override async ValueTask DisposeAsync()
   {
      if(!_isDisposed)
      {
         Contract.State.Assert(_scopeCache.Value == null, () => "Scopes must be disposed before the container");
         _isDisposed = true;
         if(_serviceProvider != null)
         {
            await _serviceProvider.DisposeAsync().caf();
         }

         _serviceProvider = null;
      }
   }
}
