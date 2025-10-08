using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Compze.Utilities.DependencyInjection.Abstractions;

public interface IDependencyInjectionContainer : IDisposable, IAsyncDisposable
{
   IRunMode RunMode { get; }
   IDependencyInjectionContainer Register(params ComponentRegistration[] registrations);
   IEnumerable<ComponentRegistration> RegisteredComponents();
   bool IsRegistered<TService>() => RegisteredComponents().Any(c => c.ServiceTypes.Contains(typeof(TService)));
   IServiceLocator ServiceLocator { get; }

   void RegisterToHandleServiceResolutionFor(IServiceCollection services)
   {
      var serviceLocator = ServiceLocator;
      foreach(var component in RegisteredComponents())
      {
         foreach(var serviceType in component.ServiceTypes)
         {
            //We handle lifetimes ourselves so registering everything as transient in the other container will avoid duplicate Dispose calls.
            services.AddTransient(serviceType, _ => component.Resolve(serviceLocator));
         }
      }
   }
}

public interface IServiceLocator : IDisposable, IAsyncDisposable
{
   TComponent Resolve<TComponent>() where TComponent : class;
   TComponent[] ResolveAll<TComponent>() where TComponent : class;
   IDisposable BeginScope();
}

public interface IRunMode
{
   bool IsTesting { get; }
}

enum Lifestyle
{
   Singleton,
   Scoped
}
