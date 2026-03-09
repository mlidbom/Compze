using Autofac;
using Autofac.Extensions.DependencyInjection;
using Compze.DependencyInjection.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Compze.DependencyInjection.Autofac.Extensions.Hosting;

public class HostableAutofacContainer(AutofacDependencyInjectionContainer compzeContainer) : IHostableContainer
{
   readonly AutofacDependencyInjectionContainer _compzeContainer = compzeContainer;

   public void UseAsServiceProviderFor(IHostBuilder hostBuilder) =>
      hostBuilder.UseServiceProviderFactory(new CompzeAutofacServiceProviderFactory(_compzeContainer));
}

class CompzeAutofacServiceProviderFactory(AutofacDependencyInjectionContainer compzeContainer) : IServiceProviderFactory<ContainerBuilder>
{
   readonly AutofacDependencyInjectionContainer _compzeContainer = compzeContainer;

   public ContainerBuilder CreateBuilder(IServiceCollection services)
   {
      var builder = ((IAutofacContainerInternals)_compzeContainer).ContainerBuilder;
      builder.Populate(services);
      return builder;
   }

   public IServiceProvider CreateServiceProvider(ContainerBuilder containerBuilder)
   {
      _ = _compzeContainer.ServiceLocator;
      return new CompzeAutofacServiceProvider(_compzeContainer);
   }
}

class CompzeAutofacServiceProvider(AutofacDependencyInjectionContainer container) : IServiceProvider, IServiceScopeFactory, ISupportRequiredService, IDisposable, IAsyncDisposable
{
   readonly AutofacDependencyInjectionContainer _container = container;

   public object? GetService(Type serviceType)
   {
      try { return _container.Resolve(serviceType); }
#pragma warning disable CA1031
      catch { return null; }
#pragma warning restore CA1031
   }

   public object GetRequiredService(Type serviceType) => _container.Resolve(serviceType);

   public IServiceScope CreateScope()
   {
      var rootScope = ((IAutofacContainerInternals)_container).LifetimeScope;
      var childScope = rootScope.BeginLifetimeScope();
      return new CompzeAutofacServiceScope(this, childScope);
   }

   public void Dispose() => _container.Dispose();
   public ValueTask DisposeAsync() => _container.DisposeAsync();
}

class CompzeAutofacServiceScope(IServiceProvider serviceProvider, ILifetimeScope scope) : IServiceScope
{
   readonly ILifetimeScope _scope = scope;
   public IServiceProvider ServiceProvider { get; } = serviceProvider;

   public void Dispose() => _scope.Dispose();
}
