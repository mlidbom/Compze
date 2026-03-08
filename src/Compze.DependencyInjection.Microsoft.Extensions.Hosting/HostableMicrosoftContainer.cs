using Compze.DependencyInjection.Extensions.Hosting;
using Compze.DependencyInjection.Microsoft;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Compze.DependencyInjection.Microsoft.Extensions.Hosting;

public class HostableMicrosoftContainer(MicrosoftDependencyInjectionContainer compzeContainer) : IHostableContainer
{
   readonly MicrosoftDependencyInjectionContainer _compzeContainer = compzeContainer;

   public void UseAsServiceProviderFor(IHostBuilder hostBuilder) =>
      hostBuilder.UseServiceProviderFactory(new CompzeMicrosoftServiceProviderFactory(_compzeContainer));
}

class CompzeMicrosoftServiceProviderFactory(MicrosoftDependencyInjectionContainer compzeContainer) : IServiceProviderFactory<IServiceCollection>
{
   readonly MicrosoftDependencyInjectionContainer _compzeContainer = compzeContainer;

   public IServiceCollection CreateBuilder(IServiceCollection services)
   {
      var compzeServices = ((IMicrosoftContainerInternals)_compzeContainer).ServiceCollection;
      foreach(var descriptor in compzeServices)
         services.Add(descriptor);
      return services;
   }

   public IServiceProvider CreateServiceProvider(IServiceCollection services)
   {
      var innerProvider = services.BuildServiceProvider(new ServiceProviderOptions
      {
         ValidateOnBuild = true,
         ValidateScopes = true
      });

      return new CompzeMicrosoftServiceProvider(_compzeContainer, innerProvider);
   }
}

class CompzeMicrosoftServiceProvider(MicrosoftDependencyInjectionContainer compzeContainer, ServiceProvider innerProvider) : IServiceProvider, IServiceScopeFactory, ISupportRequiredService, IDisposable, IAsyncDisposable
{
   readonly MicrosoftDependencyInjectionContainer _compzeContainer = compzeContainer;
   readonly ServiceProvider _innerProvider = innerProvider;

   public object? GetService(Type serviceType)
   {
      if(serviceType == typeof(IServiceScopeFactory)) return this;
      if(serviceType == typeof(IServiceProvider)) return this;
      return _innerProvider.GetService(serviceType);
   }

   public object GetRequiredService(Type serviceType)
   {
      if(serviceType == typeof(IServiceScopeFactory)) return this;
      if(serviceType == typeof(IServiceProvider)) return this;
      return _innerProvider.GetRequiredService(serviceType);
   }

   public IServiceScope CreateScope()
   {
      var innerScope = _innerProvider.CreateScope();
      var internals = (IMicrosoftContainerInternals)_compzeContainer;
      internals.PushExternalScope(innerScope);
      return new CompzeMicrosoftServiceScope(this, innerScope, internals);
   }

   public void Dispose() => _innerProvider.Dispose();
   public ValueTask DisposeAsync() => _innerProvider.DisposeAsync();
}

class CompzeMicrosoftServiceScope(CompzeMicrosoftServiceProvider serviceProvider, IServiceScope innerScope, IMicrosoftContainerInternals compzeInternals) : IServiceScope
{
   readonly IServiceScope _innerScope = innerScope;
   readonly IMicrosoftContainerInternals _compzeInternals = compzeInternals;

   public IServiceProvider ServiceProvider => serviceProvider;

   public void Dispose()
   {
      _compzeInternals.PopExternalScope();
      _innerScope.Dispose();
   }
}
