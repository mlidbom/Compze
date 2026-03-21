using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Underscore;
using Microsoft.Extensions.DependencyInjection;

namespace Compze.DependencyInjection.Microsoft.Extensions.Hosting;

public class MicrosoftServiceProviderFactory(MicrosoftContainerBuilder compzeBuilder) : IServiceProviderFactory<IServiceCollection>
{
   readonly MicrosoftContainerBuilder _compzeBuilder = compzeBuilder;

   public IServiceCollection CreateBuilder(IServiceCollection services) =>
      _compzeBuilder.CastTo<IMicrosoftBuilderInternals>()
                    .ServiceCollection
                    ._mutate(it => services.ForEach(it.Add));

   public IServiceProvider CreateServiceProvider(IServiceCollection services) =>
      _compzeBuilder.Build()
                    .CastTo<IMicrosoftContainerInternals>()
                    .ServiceProvider;
}
