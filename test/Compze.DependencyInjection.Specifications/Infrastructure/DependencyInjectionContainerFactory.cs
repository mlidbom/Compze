using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection.Autofac;
using Compze.DependencyInjection.Microsoft;
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
}
