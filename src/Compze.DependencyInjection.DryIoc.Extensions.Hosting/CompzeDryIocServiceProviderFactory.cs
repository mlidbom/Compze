using Compze.Internals.SystemCE;
using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Compze.DependencyInjection.DryIoc.Extensions.Hosting;

public class CompzeDryIocServiceProviderFactory(DryIocContainerBuilder builder) : IServiceProviderFactory<DryIocServiceProvider>
{
   readonly DryIocContainerBuilder _builder = builder;

   public DryIocServiceProvider CreateBuilder(IServiceCollection services) =>
      _builder.CastTo<IDryIocBuilderInternals>()
              .Container
              .WithDependencyInjectionAdapter(services);

   public IServiceProvider CreateServiceProvider(DryIocServiceProvider serviceProvider)
   {
      _builder.Build();
      return serviceProvider;
   }
}
