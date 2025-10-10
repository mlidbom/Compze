using System;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tests.Infrastructure.XUnit.PluggableComponents;

/// <summary>
/// Extension methods for PluggableComponentTestContext to provide convenient access to common testing utilities.
/// </summary>
public static class PluggableComponentTestContextExtensions
{
   /// <summary>
   /// Creates a DI container instance configured with the current combination's DI container type.
   /// </summary>
   /// <param name="context">The test context</param>
   /// <param name="isTesting">Whether the container is in testing mode (default: true)</param>
   /// <returns>A configured DI container instance</returns>
   public static IDependencyInjectionContainer CreateContainer(this PluggableComponentTestContext context, bool isTesting = true)
   {
      var runMode = new RunMode(isTesting: isTesting);
      
      return context.DIContainer switch
      {
         Compze.Wiring.DIContainer.Microsoft => 
            new Compze.Utilities.DependencyInjection.Microsoft.MicrosoftDependencyInjectionContainer(runMode),
         Compze.Wiring.DIContainer.SimpleInjector => 
            new Compze.Utilities.DependencyInjection.SimpleInjector.SimpleInjectorDependencyInjectionContainer(runMode),
         _ => throw new NotSupportedException($"DI Container {context.DIContainer} is not supported in tests")
      };
   }
}
