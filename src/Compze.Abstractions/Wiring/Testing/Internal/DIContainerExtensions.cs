namespace Compze.Abstractions.Wiring.Testing.Internal;

/// <summary>
/// Extension methods for DIContainer to provide convenient access to layer-specific values in tests.
/// </summary>
public static class DIContainerExtensions
{
   public static TValue ValueFor<TValue>(
      this DIContainer container,
      TValue autofac,
      TValue microsoft,
      TValue simpleInjector) where TValue : notnull
   {
      return container switch
      {
         DIContainer.Autofac   => autofac,
         DIContainer.Microsoft => microsoft,
         DIContainer.SimpleInjector   => simpleInjector,
         _                     => throw new ArgumentOutOfRangeException(nameof(container), container, $"Unsupported DI container: {container}")
      };
   }
}
