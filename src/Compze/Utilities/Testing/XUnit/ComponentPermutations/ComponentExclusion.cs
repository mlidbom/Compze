namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

/// <summary>
/// Represents an exclusion of a component, optionally with a reason.
/// Format: "ComponentName" or "ComponentName::Reason"
/// </summary>
internal class ComponentExclusion
{
   const string Separator = "::";

   public string ComponentName { get; }
   public string? Reason { get; }

   ComponentExclusion(string componentName, string? reason)
   {
      ComponentName = componentName;
      Reason = reason;
   }

   /// <summary>
   /// Parses an exclusion specification string.
   /// Supports two formats:
   /// - "ComponentName" - excludes the component without a specific reason
   /// - "ComponentName::Reason" - excludes the component with a specified reason
   /// </summary>
   public static ComponentExclusion Parse(string exclusionSpec)
   {
      var parts = exclusionSpec.Split(Separator, 2);
      var componentName = parts[0];
      var reason = parts.Length > 1 ? parts[1] : null;

      return new ComponentExclusion(componentName, reason);
   }

   /// <summary>
   /// Determines if this exclusion matches the given permutation.
   /// </summary>
   public bool Matches(ComponentsPermutation permutation) =>
      permutation.Components.Contains(ComponentName);
}
