using Compze.DependencyInjection.Extensions.Hosting;
using Compze.DependencyInjection.Wiring.Registration;
using Compze.Underscore;
using Microsoft.Extensions.Hosting;

namespace Compze.DependencyInjection.Microsoft.Extensions.Hosting;

public class MicrosoftChildContainerHostIntegration(MicrosoftContainer parentContainer) : IChildContainerHostIntegration
{
   readonly MicrosoftContainer _parentContainer = parentContainer;

   public static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<IChildContainerHostIntegration>()
                  .CreatedBy((MicrosoftContainer container) => new MicrosoftChildContainerHostIntegration(container)));

   public void UseChildContainerAsServiceProviderFor(IHostBuilder hostBuilder) =>
      _parentContainer.CreateChildContainerBuilder()
                      ._(it => new MicrosoftServiceProviderFactory(it))
                      ._(hostBuilder.UseServiceProviderFactory);
}
