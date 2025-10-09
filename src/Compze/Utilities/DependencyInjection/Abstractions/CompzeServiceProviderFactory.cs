using System;
using Microsoft.Extensions.DependencyInjection;

namespace Compze.Utilities.DependencyInjection.Abstractions;

/// <summary>
/// A service provider factory that integrates Compze's DI container with Microsoft's DI,
/// preventing double-disposal by ensuring only one container manages component lifetimes.
/// </summary>
public class CompzeServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
{
   readonly IDependencyInjectionContainer _container;

   public CompzeServiceProviderFactory(IDependencyInjectionContainer container)
   {
      _container = container;
   }

   public IServiceCollection CreateBuilder(IServiceCollection services)
   {
      // Return the services collection as-is for ASP.NET Core to add its services
      return services;
   }

   public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder)
   {
      // Build Microsoft's ServiceProvider for ASP.NET Core's services  
      var microsoftProvider = containerBuilder.BuildServiceProvider();
      
      // Wrap it to intercept resolution
      return new HybridServiceProvider(_container, microsoftProvider);
   }
}
