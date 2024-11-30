using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Composable.DependencyInjection;

public interface IDependencyInjectionContainer : IDisposable, IAsyncDisposable
{
   IRunMode RunMode { get; }
   void Register(params ComponentRegistration[] registrations);
   IEnumerable<ComponentRegistration> RegisteredComponents();
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
interface IServiceLocatorKernel
{
   TComponent Resolve<TComponent>() where TComponent : class;
}
public interface IRunMode
{
   bool IsTesting { get; }
}
public enum PersistenceLayer
{
   MicrosoftSqlServer,
   Memory,
   MySql,
   PostgreSql
}
public enum DIContainer
{
   SimpleInjector,
   Microsoft
}
enum Lifestyle
{
   Singleton,
   Scoped
}
