using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE.LinqCE;
using Microsoft.Extensions.DependencyInjection;

namespace Compze.Utilities.DependencyInjection.Abstractions;

public interface IDependencyRegistrar
{
   IDependencyRegistrar Register(params ComponentRegistration[] registrations);

   IDependencyRegistrar Register(params Action<IDependencyRegistrar>[] registrationMethods)
      => registrationMethods.ForEach(it => it(this))
                            .then(this);

   IDependencyInjectionContainer Container();
   IRunMode RunMode { get; }
}

public interface IDependencyInjectionContainer : IDisposable, IAsyncDisposable
{
   IRunMode RunMode { get; }
   IDependencyRegistrar Register();
   IDependencyInjectionContainer Register(params ComponentRegistration[] registrations);
   IEnumerable<ComponentRegistration> RegisteredComponents();
   bool IsRegistered<TService>() => RegisteredComponents().Any(c => c.ServiceTypes.Contains(typeof(TService)));
   bool IsRegistered(Type serviceType) => RegisteredComponents().Any(c => c.ServiceTypes.Contains(serviceType));
   IServiceLocator ServiceLocator { get; }

   /// <summary>
   /// Registers this container to handle service resolution for an IServiceCollection.
   /// Returns a custom IServiceProviderFactory that should be used instead of the default.
   /// This prevents double-disposal of services managed by this container.
   /// 
   /// Usage in ASP.NET Core:
   /// <code>
   /// builder.Host.UseServiceProviderFactory(new CompzeServiceProviderFactory(container));
   /// </code>
   /// </summary>
   IServiceProviderFactory<IServiceCollection> CreateServiceProviderFactory()
   {
      return new CompzeServiceProviderFactory(this);
   }
   
   /// <summary>
   /// DEPRECATED: This method causes double-disposal issues. Use CreateServiceProviderFactory() instead.
   /// Registers services from this container into a Microsoft DI ServiceCollection.
   /// WARNING: Services will be disposed twice - once by Microsoft DI and once by this container.
   /// </summary>
   [Obsolete("This method causes double-disposal. Use CreateServiceProviderFactory() instead.")]
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
   object Resolve(Type serviceType);
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
