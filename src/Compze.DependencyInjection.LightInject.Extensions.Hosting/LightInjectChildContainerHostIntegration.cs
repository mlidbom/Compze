using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection.Extensions.Hosting;
using Compze.Underscore;
using Microsoft.Extensions.Hosting;

namespace Compze.DependencyInjection.LightInject.Extensions.Hosting;

public class LightInjectChildContainerHostIntegration(LightInjectContainer parentContainer) : IChildContainerHostIntegration
{
   readonly LightInjectContainer _parentContainer = parentContainer;

   public static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<IChildContainerHostIntegration>()
                  .CreatedBy((LightInjectContainer container) => new LightInjectChildContainerHostIntegration(container)));

   public void UseChildContainerAsServiceProviderFor(IHostBuilder hostBuilder) =>
      _parentContainer.CreateChildContainerBuilder()
                      ._(it => new CompzeLightInjectServiceProviderFactory(it))
                      ._(hostBuilder.UseServiceProviderFactory);
}
