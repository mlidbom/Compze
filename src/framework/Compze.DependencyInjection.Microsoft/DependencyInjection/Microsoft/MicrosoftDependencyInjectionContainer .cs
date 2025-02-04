﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compze.Contracts;
using Compze.SystemCE;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Microsoft.Extensions.DependencyInjection;

namespace Compze.DependencyInjection.Microsoft;

public sealed class MicrosoftDependencyInjectionContainer : IDependencyInjectionContainer, IServiceLocator, IServiceLocatorKernel
{
   readonly IServiceCollection _services;
   readonly List<ComponentRegistration> _registeredComponents = [];
   ServiceProvider? _serviceProvider;
   bool _isDisposed;

   readonly AsyncLocal<IServiceScope?> _scopeCache = new();

   internal MicrosoftDependencyInjectionContainer(IRunMode runMode)
   {
      RunMode = runMode;
      _services = new ServiceCollection();
   }

   public IRunMode RunMode { get; }

   public void Register(params ComponentRegistration[] registrations)
   {
      _registeredComponents.AddRange(registrations);

      foreach(var componentRegistration in registrations)
      {
         var lifetime = componentRegistration.Lifestyle switch
         {
            Lifestyle.Singleton => ServiceLifetime.Singleton,
            Lifestyle.Scoped => ServiceLifetime.Scoped,
            _ => throw new ArgumentOutOfRangeException(nameof(componentRegistration.Lifestyle))
         };

         if(componentRegistration.InstantiationSpec.SingletonInstance != null)
         {
            Assert.Argument.Is(lifetime == ServiceLifetime.Singleton, () => $"{componentRegistration.ServiceTypes.First().FullName} tried to register using an Instance and lifestyle: {lifetime}. Instance can only be used with {nameof(Lifestyle.Singleton)}");
            foreach(var serviceType in componentRegistration.ServiceTypes)
            {
               _services.AddSingleton(serviceType, componentRegistration.InstantiationSpec.SingletonInstance);
            }
         } else
         {
            var firstServiceType = componentRegistration.ServiceTypes.First();
            var primaryDescriptor = new ServiceDescriptor(firstServiceType, _ =>  componentRegistration.InstantiationSpec.RunFactoryMethod(this), lifetime);

            _services.Add(primaryDescriptor);

            foreach(var serviceType in componentRegistration.ServiceTypes.Skip(1))
            {
               _services.Add(new ServiceDescriptor(serviceType, sp => sp.GetService(firstServiceType)!, lifetime));
            }
         }
      }
   }

   public IEnumerable<ComponentRegistration> RegisteredComponents() => _registeredComponents;

   public IServiceLocator ServiceLocator
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

   public void Dispose()
   {
      Assert.State.Is(_scopeCache.Value == null, () => "Scopes must be disposed before the container");
      if(_isDisposed) return;
      _isDisposed = true;
      _serviceProvider?.Dispose();
      _serviceProvider = null;
   }

   public async ValueTask DisposeAsync()
   {
      Assert.State.Is(_scopeCache.Value == null, () => "Scopes must be disposed before the container");
      if(!_isDisposed)
      {
         _isDisposed = true;
         if(_serviceProvider != null)
         {
            await _serviceProvider.DisposeAsync().CaF();
         }
         _serviceProvider = null;
      }
      await Task.CompletedTask.CaF();
   }
}