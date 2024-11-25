using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Composable.DependencyInjection;

public interface IDependencyInjectionContainer : IDisposable, IAsyncDisposable
{
   IRunMode RunMode { get; }
   void Register(params ComponentRegistration[] registrations);
   IEnumerable<ComponentRegistration> RegisteredComponents();
   IServiceLocator CreateServiceLocator();

   void RegisterServicesInIServiceCollection(IServiceCollection services)
   {
      foreach(var component in RegisteredComponents())
      {
         var serviceLocator = CreateServiceLocator();
         switch(component.Lifestyle)
         {
            case Lifestyle.Singleton:
               foreach(var serviceType in component.ServiceTypes)
               {
                  services.AddSingleton(serviceType, _ => component.Resolve(serviceLocator));
               }

               break;
            case Lifestyle.Scoped:
               foreach(var serviceType in component.ServiceTypes)
               {
                  services.AddScoped(serviceType, _ => component.Resolve(serviceLocator));
               }

               break;
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
   MicrosoftSQLServer,
   Memory,
   MySql,
   PostgreSql,
   Oracle,
   IBMDB2
}

public enum DIContainer
{
   Composable, SimpleInjector, WindsorCastle, Microsoft
}

enum Lifestyle
{
   Singleton,
   Scoped
}
