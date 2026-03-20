using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection.Extensions.Hosting;
using Compze.Internals.SystemCE.LinqCE;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Compze.DependencyInjection.Microsoft.Extensions.Hosting;

public class HostableMicrosoftContainer(MicrosoftContainerBuilder compzeBuilder) : IHostableContainer
{
   readonly MicrosoftContainerBuilder _compzeBuilder = compzeBuilder;

   public void UseAsServiceProviderFor(IHostBuilder hostBuilder) =>
      hostBuilder.UseServiceProviderFactory(new CompzeMicrosoftServiceProviderFactory(_compzeBuilder));
}

public class MicrosoftChildContainerHostIntegration(MicrosoftContainer parentContainer) : IChildContainerHostIntegration
{
   readonly MicrosoftContainer _parentContainer = parentContainer;

   public static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<IChildContainerHostIntegration>()
                  .CreatedBy((MicrosoftContainer container) => new MicrosoftChildContainerHostIntegration(container)));

   public void UseChildContainerAsServiceProviderFor(IHostBuilder hostBuilder)
   {
      var childBuilder = _parentContainer.CreateChildContainerBuilder();
      hostBuilder.UseServiceProviderFactory(new CompzeMicrosoftServiceProviderFactory(childBuilder));
   }
}

class CompzeMicrosoftServiceProviderFactory(MicrosoftContainerBuilder compzeBuilder) : IServiceProviderFactory<IServiceCollection>
{
   readonly MicrosoftContainerBuilder _compzeBuilder = compzeBuilder;
   MicrosoftContainer? _container;

   public IServiceCollection CreateBuilder(IServiceCollection services)
   {
      var compzeServices = ((IMicrosoftBuilderInternals)_compzeBuilder).ServiceCollection;
      services.ForEach(compzeServices.Add);
      return compzeServices;
   }

   public IServiceProvider CreateServiceProvider(IServiceCollection services)
   {
      _container = (MicrosoftContainer)((IContainerBuilder)_compzeBuilder).Build();
      return new CompzeMicrosoftServiceProvider(_container);
   }
}

class CompzeMicrosoftServiceProvider(MicrosoftContainer container) : IServiceProvider, ISupportRequiredService, IDisposable, IAsyncDisposable
{
   readonly MicrosoftContainer _container = container;
   IServiceProvider Provider => ((IMicrosoftContainerInternals)_container).ServiceProvider;

   public object? GetService(Type serviceType) => Provider.GetService(serviceType);
   public object GetRequiredService(Type serviceType) => Provider.GetRequiredService(serviceType);

   public void Dispose() => _container.Dispose();
   public ValueTask DisposeAsync() => _container.DisposeAsync();
}
