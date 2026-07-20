using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE;
using DryIoc.Microsoft.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Compze.DependencyInjection.DryIoc.Extensions.Hosting;

public class CompzeDryIocServiceProviderFactory(DryIocContainerBuilder builder, ContainerOptions? options = null) : IServiceProviderFactory<DryIocServiceProvider>
{
   readonly DryIocContainerBuilder _builder = builder;
   readonly ContainerOptions _options = options ?? ContainerOptions.Default;

   public DryIocServiceProvider CreateBuilder(IServiceCollection services) =>
      _builder.CastTo<IDryIocBuilderInternals>()
              .Container
              .WithDependencyInjectionAdapter(services);

   public IServiceProvider CreateServiceProvider(DryIocServiceProvider serviceProvider)
   {
      _builder.Build(_options);
      return serviceProvider;
   }
}
