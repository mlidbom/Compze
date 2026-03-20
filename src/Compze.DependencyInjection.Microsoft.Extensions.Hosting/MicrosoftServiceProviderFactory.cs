using Compze.Internals.SystemCE.LinqCE;
using Microsoft.Extensions.DependencyInjection;

namespace Compze.DependencyInjection.Microsoft.Extensions.Hosting;

public class MicrosoftServiceProviderFactory(MicrosoftContainerBuilder compzeBuilder) : IServiceProviderFactory<IServiceCollection>
{
   readonly MicrosoftContainerBuilder _compzeBuilder = compzeBuilder;

   public IServiceCollection CreateBuilder(IServiceCollection services)
   {
      var compzeServices = ((IMicrosoftBuilderInternals)_compzeBuilder).ServiceCollection;
      services.ForEach(compzeServices.Add);
      return compzeServices;
   }

   public IServiceProvider CreateServiceProvider(IServiceCollection services)
   {
      var container = _compzeBuilder.Build();
      return ((IMicrosoftContainerInternals)container).ServiceProvider;
   }
}
