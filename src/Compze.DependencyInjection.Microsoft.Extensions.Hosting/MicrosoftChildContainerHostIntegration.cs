using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection.Extensions.Hosting;
using Microsoft.Extensions.Hosting;

namespace Compze.DependencyInjection.Microsoft.Extensions.Hosting;

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
      hostBuilder.UseServiceProviderFactory(new MicrosoftServiceProviderFactory(childBuilder));
   }
}