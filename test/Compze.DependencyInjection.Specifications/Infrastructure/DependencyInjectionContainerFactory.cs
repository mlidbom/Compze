using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection.Autofac;
using Compze.DependencyInjection.Autofac.Extensions.Hosting;
using Compze.DependencyInjection.Extensions.Hosting;
using Compze.DependencyInjection.Microsoft;
using Compze.DependencyInjection.Microsoft.Extensions.Hosting;
using Compze.xUnitMatrix;

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

   public static (IContainerBuilder Builder, IHostableContainer Hostable) CreateHostableContainerBuilder() =>
      CurrentDIContainer switch
      {
         DIContainer.Microsoft => CreateHostablePair(new MicrosoftContainerBuilder()),
         DIContainer.Autofac   => CreateHostablePair(new AutofacContainerBuilder()),
         _                     => throw new ArgumentOutOfRangeException()
      };

   static (IContainerBuilder, IHostableContainer) CreateHostablePair(MicrosoftContainerBuilder builder) =>
      (builder, new HostableMicrosoftContainer(builder));

   static (IContainerBuilder, IHostableContainer) CreateHostablePair(AutofacContainerBuilder builder) =>
      (builder, new HostableAutofacContainer(builder));
}
