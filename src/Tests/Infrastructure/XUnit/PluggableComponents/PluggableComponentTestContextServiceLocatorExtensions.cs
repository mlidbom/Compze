using System;
using Compze.Tessaging.Hosting.Testing;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Logging;

namespace Compze.Tests.Infrastructure.XUnit.PluggableComponents;

/// <summary>
/// Extension methods for PluggableComponentTestContext to provide service locator functionality.
/// </summary>
public static class PluggableComponentTestContextServiceLocatorExtensions
{
   static ILogger Log => CompzeLogger.For(typeof(PluggableComponentTestContextServiceLocatorExtensions));
   /// <summary>
   /// Creates a testing service locator configured for the current pluggable component combination.
   /// The service locator should be disposed after use.
   /// </summary>
   public static IServiceLocator CreateServiceLocator(this PluggableComponentTestContext context, Action<IDependencyRegistrar>? configureContainer = null)
   {
      // Set the TestEnv context so it can read the combination
      TestEnv.SetXunitTestContext(context.Combination);
      Log.NCrunch(nameof(CreateServiceLocator));

      return TestWiringHelper.SetupTestingServiceLocator(configureContainer);
   }
}
