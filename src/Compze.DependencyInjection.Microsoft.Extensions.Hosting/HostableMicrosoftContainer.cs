using Compze.DependencyInjection.Extensions.Hosting;
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

   public IServiceScope CreateScope() => _innerProvider.CreateScope();

   public void Dispose()
   {
      _innerProvider.Dispose();
      _compzeContainer.Dispose();
   }

   public async ValueTask DisposeAsync()
   {
      await _innerProvider.DisposeAsync().ConfigureAwait(false);
      await _compzeContainer.DisposeAsync().ConfigureAwait(false);
   }
}
