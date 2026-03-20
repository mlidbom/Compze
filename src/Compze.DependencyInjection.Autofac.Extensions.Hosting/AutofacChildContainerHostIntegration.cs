using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection.Extensions.Hosting;
using Compze.Underscore;
using Microsoft.Extensions.Hosting;

namespace Compze.DependencyInjection.Autofac.Extensions.Hosting;

public class AutofacChildContainerHostIntegration(AutofacContainer parentContainer) : IChildContainerHostIntegration
{
   readonly AutofacContainer _parentContainer = parentContainer;

   public static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<IChildContainerHostIntegration>()
                  .CreatedBy((AutofacContainer container) => new AutofacChildContainerHostIntegration(container)));

   public void UseChildContainerAsServiceProviderFor(IHostBuilder hostBuilder) =>
      _parentContainer.CreateChildContainerBuilder()
                      ._(it => new CompzeAutofacServiceProviderFactory(it))
                      ._(hostBuilder.UseServiceProviderFactory);
}