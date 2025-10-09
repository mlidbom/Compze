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
      if(serviceType == typeof(IServiceProvider) || serviceType == typeof(IServiceScopeFactory)) return this;

      if(_compzeContainer.IsRegistered(serviceType))
      {
         return _compzeContainer.ServiceLocator.Resolve(serviceType);
      }

      return _microsoftProvider.GetService(serviceType);
   }

   public object GetRequiredService(Type serviceType)
   {
      var service = GetService(serviceType);
      if(service == null)
      {
         throw new InvalidOperationException($"No service for type '{serviceType}' has been registered.");
      }

      return service;
   }

   public IServiceScope CreateScope()
   {
      var compzeScope = _compzeContainer.ServiceLocator.BeginScope();
      var microsoftScope = _microsoftScopeFactory.CreateScope();

      return new HybridServiceScope(_compzeContainer, microsoftScope, compzeScope);
   }

   public void Dispose()
   {
      if(_microsoftProvider is IDisposable disposable)
      {
         disposable.Dispose();
      }
   }

   public async ValueTask DisposeAsync()
   {
      if(_microsoftProvider is IAsyncDisposable asyncDisposable)
      {
         await asyncDisposable.DisposeAsync().ConfigureAwait(false);
      } else if(_microsoftProvider is IDisposable disposable)
      {
         disposable.Dispose();
      }
   }
}
