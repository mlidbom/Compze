using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compze.Utilities.Contracts;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;
using Microsoft.Extensions.DependencyInjection;

namespace Compze.Utilities.DependencyInjection.Microsoft;

public sealed class MicrosoftDependencyInjectionContainer : DependencyInjectionContainerBase, IServiceLocator, IServiceLocatorKernel
{
   readonly IServiceCollection _services;
   ServiceProvider? _serviceProvider;
   bool _isDisposed;

   readonly AsyncLocal<IServiceScope?> _scopeCache = new();

   public MicrosoftDependencyInjectionContainer(IRunMode runMode) : base(runMode) =>
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
            var primaryDescriptor = new ServiceDescriptor(firstServiceType, _ => registration.InstantiationSpec.RunFactoryMethod(this), lifetime);

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
         _serviceProvider ??= _services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
         return this;
      }
   }

   IServiceProvider CurrentProvider()
   {
      Assert.State.IsNotDisposed(_isDisposed, this);
      return _scopeCache.Value != null ? _scopeCache.Value.ServiceProvider : _serviceProvider.NotNull();
   }

   public TComponent Resolve<TComponent>() where TComponent : class
   {
      Assert.State.IsNotDisposed(_isDisposed, this);
      return CurrentProvider().GetRequiredService<TComponent>();
   }

   public object Resolve(Type serviceType)
   {
      Assert.State.IsNotDisposed(_isDisposed, this);
      return CurrentProvider().GetRequiredService(serviceType);
   }

   public TComponent[] ResolveAll<TComponent>() where TComponent : class
   {
      Assert.State.IsNotDisposed(_isDisposed, this);
      return CurrentProvider().GetServices<TComponent>().ToArray();
   }

   TComponent IServiceLocatorKernel.Resolve<TComponent>()
   {
      Assert.State.IsNotDisposed(_isDisposed, this);
      return CurrentProvider().GetRequiredService<TComponent>();
   }

   IDisposable IServiceLocator.BeginScope()
   {
      Assert.State.IsNotDisposed(_isDisposed, this)
            .Is(_scopeCache.Value == null, () => "Scope already exists. Nested scopes are not supported.");

      _scopeCache.Value = CurrentProvider().CreateAsyncScope();

      return DisposableCE.Create(() =>
      {
         Assert.State.Is(_scopeCache.Value != null, () => "Attempt to dispose scope from a context that is not within the scope.");
         _scopeCache.Value.Dispose();
         _scopeCache.Value = null;
      });
   }

   [SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "CA1063 reports 'Dispose should be public and sealed' but incorrectly flags the protected Dispose(bool) override instead of recognizing this sealed class already has a sealed public Dispose() inherited from the base class.")]
   protected override void Dispose(bool disposing)
   {
      if(disposing)
      {
         Assert.State.Is(_scopeCache.Value == null, () => "Scopes must be disposed before the container");
         if(!_isDisposed)
         {
            _isDisposed = true;
            _serviceProvider?.Dispose();
            _serviceProvider = null;
         }
      }

      base.Dispose(disposing);
   }

   protected override async ValueTask DisposeAsyncCore()
   {
      Assert.State.Is(_scopeCache.Value == null, () => "Scopes must be disposed before the container");
      if(!_isDisposed)
      {
         _isDisposed = true;
         if(_serviceProvider != null)
         {
            await _serviceProvider.DisposeAsync().caf();
         }

         _serviceProvider = null;
      }

      await base.DisposeAsyncCore().caf();
   }
}
