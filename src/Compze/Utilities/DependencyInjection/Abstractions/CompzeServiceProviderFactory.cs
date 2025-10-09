using System;
using Microsoft.Extensions.DependencyInjection;

namespace Compze.Utilities.DependencyInjection.Abstractions;

/// <summary>
/// A service provider factory that integrates Compze's DI container with Microsoft's DI,
/// preventing double-disposal by ensuring only one container manages component lifetimes.
/// </summary>
public class CompzeServiceProviderFactory(IDependencyInjectionContainer container) : IServiceProviderFactory<IServiceCollection>
{
   readonly IDependencyInjectionContainer _container = container;

   public IServiceCollection CreateBuilder(IServiceCollection services) => services;

   public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder) =>
      new HybridServiceProvider(_container, containerBuilder.BuildServiceProvider());
}
