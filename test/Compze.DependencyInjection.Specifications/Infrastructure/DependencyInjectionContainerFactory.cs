using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection.Autofac;
using Compze.DependencyInjection.Autofac.Extensions.Hosting;
using Compze.DependencyInjection.Extensions.Hosting;
using Compze.DependencyInjection.Microsoft;
using Compze.DependencyInjection.Microsoft.Extensions.Hosting;
using Compze.DependencyInjection.SimpleInjector;
using Compze.xUnitMatrix;

namespace Compze.DependencyInjection.Specifications.Infrastructure;

static class DependencyInjectionContainerFactory
{
   static DIContainer CurrentDIContainer => (DIContainer)ComponentCombination.Current.Components[0];

   public static IDependencyInjectionContainer CreateContainer() =>
      CurrentDIContainer switch
      {
         DIContainer.SimpleInjector => new SimpleInjectorDependencyInjectionContainer(),
         DIContainer.Microsoft      => new MicrosoftDependencyInjectionContainer(),
         DIContainer.Autofac        => new AutofacDependencyInjectionContainer(),
         _                          => throw new ArgumentOutOfRangeException()
      };

   public static (IDependencyInjectionContainer Container, IHostableContainer Hostable) CreateHostableContainer() =>
      CurrentDIContainer switch
      {
#pragma warning disable CA2000
         DIContainer.Microsoft => CreateHostablePair(new MicrosoftDependencyInjectionContainer()),
         DIContainer.Autofac   => CreateHostablePair(new AutofacDependencyInjectionContainer()),
         _                     => throw new ArgumentOutOfRangeException()
#pragma warning restore CA2000
      };

   static (IDependencyInjectionContainer, IHostableContainer) CreateHostablePair(MicrosoftDependencyInjectionContainer container) =>
      (container, new HostableMicrosoftContainer(container));

   static (IDependencyInjectionContainer, IHostableContainer) CreateHostablePair(AutofacDependencyInjectionContainer container) =>
      (container, new HostableAutofacContainer(container));
}
