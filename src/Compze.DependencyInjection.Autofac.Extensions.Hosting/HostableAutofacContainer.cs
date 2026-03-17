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

class CompzeAutofacServiceProvider(AutofacDependencyInjectionContainer compzeContainer) : IServiceProvider, ISupportRequiredService, IDisposable, IAsyncDisposable
{
   readonly AutofacDependencyInjectionContainer _compzeContainer = compzeContainer;
   IContainer Container => ((IAutofacContainerInternals)_compzeContainer).Container;

   public object? GetService(Type serviceType) => Container.ResolveOptional(serviceType);
   public object GetRequiredService(Type serviceType) => Container.Resolve(serviceType);

   public void Dispose() => _compzeContainer.Dispose();
   public ValueTask DisposeAsync() => _compzeContainer.DisposeAsync();
}
