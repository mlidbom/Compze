using Compze.Hosting.Testing;
using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection.Autofac;
using Compze.DependencyInjection.Autofac.Extensions.Hosting;
using Compze.DependencyInjection.DryIoc;
using Compze.DependencyInjection.DryIoc.Extensions.Hosting;
using Compze.DependencyInjection.LightInject;
using Compze.DependencyInjection.LightInject.Extensions.Hosting;
using Compze.DependencyInjection.Microsoft;
using Compze.DependencyInjection.Microsoft.Extensions.Hosting;
using Compze.xUnitMatrix;
using Microsoft.Extensions.Hosting;

namespace Compze.DependencyInjection.Specifications.Infrastructure;

static class DependencyInjectionContainerFactory
{
   static DIContainer CurrentDIContainer => (DIContainer)MatrixCombination.Current.DimensionValues[0];

   public static IContainerBuilder CreateContainerBuilder() =>
      CurrentDIContainer switch
      {
         DIContainer.Microsoft      => new MicrosoftContainerBuilder(),
         DIContainer.Autofac        => new AutofacContainerBuilder(),
         DIContainer.DryIoc         => new DryIocContainerBuilder(),
         DIContainer.LightInject    => new LightInjectContainerBuilder(),
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
         case DryIocContainerBuilder dryIocBuilder:
            hostBuilder.UseServiceProviderFactory(new CompzeDryIocServiceProviderFactory(dryIocBuilder));
            break;
         case LightInjectContainerBuilder lightInjectBuilder:
            hostBuilder.UseServiceProviderFactory(new CompzeLightInjectServiceProviderFactory(lightInjectBuilder));
            break;
         default:
            throw new ArgumentOutOfRangeException(nameof(builder));
      }
   }
}
