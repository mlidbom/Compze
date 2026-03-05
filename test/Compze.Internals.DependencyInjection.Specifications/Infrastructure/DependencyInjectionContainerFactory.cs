using Compze.Core.Wiring.Testing.Internal;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.DependencyInjection.Microsoft;
using Compze.Utilities.DependencyInjection.SimpleInjector;
using Compze.xUnitMatrix;

namespace Compze.Internals.DependencyInjection.Specifications.Infrastructure;

static class DependencyInjectionContainerFactory
{
   static DIContainer CurrentDIContainer => (DIContainer)ComponentCombination.Current.Components[0];

   public static IDependencyInjectionContainer CreateContainer() =>
      CurrentDIContainer switch
      {
         DIContainer.SimpleInjector => new SimpleInjectorDependencyInjectionContainer(),
         DIContainer.Microsoft      => new MicrosoftDependencyInjectionContainer(),
         _                          => throw new ArgumentOutOfRangeException()
      };
}
