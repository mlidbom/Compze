using Autofac;
using Autofac.Extensions.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Compze.DependencyInjection.Autofac.Extensions.Hosting;

public class HostableAutofacContainer(AutofacContainerBuilder compzeBuilder) : IHostableContainer
{
   readonly AutofacContainerBuilder _compzeBuilder = compzeBuilder;

   public void UseAsServiceProviderFor(IHostBuilder hostBuilder) =>
      hostBuilder.UseServiceProviderFactory(new CompzeAutofacServiceProviderFactory(_compzeBuilder));
}

class CompzeAutofacServiceProviderFactory(AutofacContainerBuilder compzeBuilder) : IServiceProviderFactory<ContainerBuilder>
{
   readonly AutofacContainerBuilder _compzeBuilder = compzeBuilder;
   AutofacContainer? _builtContainer;

   public ContainerBuilder CreateBuilder(IServiceCollection services)
   {
      var builder = ((IAutofacBuilderInternals)_compzeBuilder).ContainerBuilder;
      builder.Populate(services);
      return builder;
   }

   public IServiceProvider CreateServiceProvider(ContainerBuilder containerBuilder)
   {
      _builtContainer = (AutofacContainer)((IContainerBuilder)_compzeBuilder).Build();
      return new CompzeAutofacServiceProvider(_builtContainer);
   }
}

class CompzeAutofacServiceProvider(AutofacContainer container) : IServiceProvider, ISupportRequiredService, IDisposable, IAsyncDisposable
{
   readonly AutofacContainer _container = container;
   IContainer Container => ((IAutofacContainerInternals)_container).Container;

   public object? GetService(Type serviceType) => Container.ResolveOptional(serviceType);
   public object GetRequiredService(Type serviceType) => Container.Resolve(serviceType);

   public void Dispose() => _container.Dispose();
   public ValueTask DisposeAsync() => _container.DisposeAsync();
}
