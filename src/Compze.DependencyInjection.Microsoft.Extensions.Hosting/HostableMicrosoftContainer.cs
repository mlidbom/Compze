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
      foreach(var descriptor in services)
         compzeServices.Add(descriptor);
      return compzeServices;
   }

   public IServiceProvider CreateServiceProvider(IServiceCollection services)
   {
      _ = _compzeContainer.ServiceLocator;
      return new CompzeMicrosoftServiceProvider(_compzeContainer);
   }
}

class CompzeMicrosoftServiceProvider(MicrosoftDependencyInjectionContainer compzeContainer) : IServiceProvider, ISupportRequiredService, IDisposable, IAsyncDisposable
{
   readonly MicrosoftDependencyInjectionContainer _compzeContainer = compzeContainer;
   IServiceProvider Provider => ((IMicrosoftContainerInternals)_compzeContainer).ServiceProvider;

   public object? GetService(Type serviceType) => Provider.GetService(serviceType);
   public object GetRequiredService(Type serviceType) => Provider.GetRequiredService(serviceType);

   public void Dispose() => _compzeContainer.Dispose();
   public ValueTask DisposeAsync() => _compzeContainer.DisposeAsync();
}
