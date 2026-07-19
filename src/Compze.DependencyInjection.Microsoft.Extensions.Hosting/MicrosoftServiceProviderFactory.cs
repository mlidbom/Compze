using Compze.DependencyInjection.Wiring.Registration;
using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Underscore;
using Microsoft.Extensions.DependencyInjection;

namespace Compze.DependencyInjection.Microsoft.Extensions.Hosting;

public class MicrosoftServiceProviderFactory(MicrosoftContainerBuilder compzeBuilder, ContainerOptions? options = null) : IServiceProviderFactory<IServiceCollection>
{
   readonly MicrosoftContainerBuilder _compzeBuilder = compzeBuilder;
   readonly ContainerOptions _options = options ?? ContainerOptions.Default;

   public IServiceCollection CreateBuilder(IServiceCollection services) =>
      _compzeBuilder.CastTo<IMicrosoftBuilderInternals>()
                    .ServiceCollection
                    ._mutate(it => services.ForEach(it.Add));

   public IServiceProvider CreateServiceProvider(IServiceCollection services) =>
      _compzeBuilder.Build(_options)
                    .CastTo<IMicrosoftContainerInternals>()
                    .ServiceProvider;
}
