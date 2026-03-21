using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection.Autofac;
using Compze.DependencyInjection.Autofac.Extensions.Hosting;
using Compze.DependencyInjection.Microsoft;
using Compze.DependencyInjection.Microsoft.Extensions.Hosting;
using Compze.xUnitMatrix;
using Microsoft.Extensions.Hosting;

namespace Compze.DependencyInjection.Specifications.Infrastructure;

static class DependencyInjectionContainerFactory
{
   static DIContainer CurrentDIContainer => (DIContainer)MatrixCombination.Current.Components[0];

   public static IContainerBuilder CreateContainerBuilder() =>
      CurrentDIContainer switch
      {
         DIContainer.Microsoft      => new MicrosoftContainerBuilder(),
         DIContainer.Autofac        => new AutofacContainerBuilder(),
         _                          => throw new ArgumentOutOfRangeException()
      };

   public static void UseAsServiceProviderFor(IContainerBuilder builder, IHostBuilder hostBuilder)
   {
      switch(builder)
      {
         case MicrosoftContainerBuilder microsoftBuilder:
            hostBuilder.UseServiceProviderFactory(new MicrosoftServiceProviderFactory(microsoftBuilder));
            break;
         case AutofacContainerBuilder autofacBuilder:
            hostBuilder.UseServiceProviderFactory(new CompzeAutofacServiceProviderFactory(autofacBuilder));
            break;
         default:
            throw new ArgumentOutOfRangeException(nameof(builder));
      }
   }
}
