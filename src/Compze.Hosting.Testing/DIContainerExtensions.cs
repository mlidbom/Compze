namespace Compze.Hosting.Testing;

/// <summary>
/// Extension methods for DIContainer to provide convenient access to layer-specific values in tests.
/// </summary>
public static class DIContainerExtensions
{
   public static TValue ValueFor<TValue>(
      this DIContainer container,
      TValue autofac,
      TValue microsoft,
      TValue dryIoc) where TValue : notnull =>
      container.ValueFor(autofac, microsoft, dryIoc, dryIoc);

   public static TValue ValueFor<TValue>(
      this DIContainer container,
      TValue autofac,
      TValue microsoft,
      TValue dryIoc,
      TValue lightInject) where TValue : notnull
   {
      return container switch
      {
         DIContainer.Autofac      => autofac,
         DIContainer.Microsoft    => microsoft,
         DIContainer.DryIoc       => dryIoc,
         DIContainer.LightInject  => lightInject,
         _                        => throw new ArgumentOutOfRangeException(nameof(container), container, $"Unsupported DI container: {container}")
      };
   }
}
