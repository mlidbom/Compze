using System;
using Compze.Tests.Infrastructure;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Teventive.EventStore.Abstractions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tests.Infrastructure.XUnit;

/// <summary>
/// Extension methods for PluggableComponentTestContext to provide service locator functionality.
/// </summary>
public static class PluggableComponentTestContextServiceLocatorExtensions
{
   /// <summary>
   /// Creates a testing service locator configured for the current pluggable component combination.
   /// The service locator should be disposed after use.
   /// </summary>
   public static IServiceLocator CreateServiceLocator(this PluggableComponentTestContext context, Action<IDependencyRegistrar>? configureContainer = null)
   {
      // Set the TestEnv context so it can read the combination
      TestEnv.SetTestContext(context.Combination);

      return TestWiringHelper.SetupTestingServiceLocator(configureContainer);
   }
}
