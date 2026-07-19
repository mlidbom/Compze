using Compze.DependencyInjection.Extensions.Hosting;
using Compze.DependencyInjection.Wiring.Registration;
using Compze.Underscore;
using Microsoft.Extensions.Hosting;

namespace Compze.DependencyInjection.DryIoc.Extensions.Hosting;

public class DryIocChildContainerHostIntegration(DryIocContainer parentContainer) : IChildContainerHostIntegration
{
   readonly DryIocContainer _parentContainer = parentContainer;

   public static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<IChildContainerHostIntegration>()
                  .CreatedBy((DryIocContainer container) => new DryIocChildContainerHostIntegration(container)));

   public void UseChildContainerAsServiceProviderFor(IHostBuilder hostBuilder) =>
      _parentContainer.CreateChildContainerBuilder()
                      ._(it => new CompzeDryIocServiceProviderFactory(it))
                      ._(hostBuilder.UseServiceProviderFactory);
}
