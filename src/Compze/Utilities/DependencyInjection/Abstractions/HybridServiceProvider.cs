using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Compze.Utilities.DependencyInjection.Abstractions;

/// <summary>
/// A service provider that tries to resolve from Compze's container first,
/// falling back to Microsoft's container for ASP.NET Core services.
/// Only Microsoft's provider is disposed (by ASP.NET Core), preventing double-disposal.
/// </summary>
class HybridServiceProvider : IServiceProvider, ISupportRequiredService, IServiceScopeFactory, IDisposable, IAsyncDisposable
{
   readonly IDependencyInjectionContainer _compzeContainer;
   readonly IServiceProvider _microsoftProvider;
   readonly IServiceScopeFactory _microsoftScopeFactory;

   public HybridServiceProvider(IDependencyInjectionContainer compzeContainer, IServiceProvider microsoftProvider)
   {
      _compzeContainer = compzeContainer;
      _microsoftProvider = microsoftProvider;
      _microsoftScopeFactory = microsoftProvider.GetRequiredService<IServiceScopeFactory>();
   }

   public object? GetService(Type serviceType)
   {
      // Special services that should come from this provider
      if (serviceType == typeof(IServiceProvider)) return this;
      if (serviceType == typeof(IServiceScopeFactory)) return this;
      
      // Try Compze container first - check if the service type is registered
      if (_compzeContainer.RegisteredComponents().Any(c => c.ServiceTypes.Contains(serviceType)))
      {
         try
         {
            var method = typeof(IServiceLocator).GetMethod(nameof(IServiceLocator.Resolve))!
                                                .MakeGenericMethod(serviceType);
            return method.Invoke(_compzeContainer.ServiceLocator, null);
         }
         catch
         {
            // Fall through to Microsoft provider
         }
      }
      
      // Fall back to Microsoft provider for ASP.NET Core services
      return _microsoftProvider.GetService(serviceType);
   }

   public object GetRequiredService(Type serviceType)
   {
      var service = GetService(serviceType);
      if (service == null)
      {
         throw new InvalidOperationException($"No service for type '{serviceType}' has been registered.");
      }
      return service;
   }

   public IServiceScope CreateScope()
   {
      // Create a scope in both containers
      var compzeScope = _compzeContainer.ServiceLocator.BeginScope();
      var microsoftScope = _microsoftScopeFactory.CreateScope();
      
      return new HybridServiceScope(_compzeContainer, microsoftScope, compzeScope);
   }

   public void Dispose()
   {
      // Only dispose Microsoft's provider (ASP.NET Core services)
      // Our container is disposed separately by the application
      if (_microsoftProvider is IDisposable disposable)
      {
         disposable.Dispose();
      }
   }

   public async ValueTask DisposeAsync()
   {
      // Only dispose Microsoft's provider (ASP.NET Core services)
      if (_microsoftProvider is IAsyncDisposable asyncDisposable)
      {
         await asyncDisposable.DisposeAsync().ConfigureAwait(false);
      }
      else if (_microsoftProvider is IDisposable disposable)
      {
         disposable.Dispose();
      }
   }
}
