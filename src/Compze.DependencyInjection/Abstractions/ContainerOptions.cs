namespace Compze.DependencyInjection.Abstractions;

public class ContainerOptions
{
   /// <summary>
   /// When <c>true</c>, scoped services may be resolved from the root container without
   /// creating a scope first. This is the broken-by-design default behavior of the
   /// Microsoft DI container.
   /// <para>Default is <c>false</c> — resolving scoped services from root throws, which is the
   /// correct behavior that catches real bugs (e.g. DbContext promoted to effective singleton).</para>
   /// <para>Set to <c>true</c> only for Microsoft DI compliance tests or third-party library compatibility.</para>
   /// </summary>
   public bool AllowScopedResolutionFromRoot { get; init; }

   public static ContainerOptions Default { get; } = new();
}
