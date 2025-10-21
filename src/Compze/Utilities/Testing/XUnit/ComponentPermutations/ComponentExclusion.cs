namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

/// <summary>
/// Represents an exclusion of a component with a reason.
/// Format: "ComponentName::Reason"
/// </summary>
internal class ComponentExclusion
{
   const string Separator = "::";

   public string ComponentName { get; }
   public string Reason { get; }

   ComponentExclusion(string componentName, string reason)
   {
      ComponentName = componentName;
      Reason = reason;
   }

   /// <summary>
   /// Parses an exclusion specification string.
   /// Format: "ComponentName::Reason"
   /// The reason is mandatory to provide context for why the component is excluded.
   /// </summary>
   public static ComponentExclusion Parse(string exclusionSpec)
   {
      var parts = exclusionSpec.Split(Separator, 2);
      
      if(parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
         throw new ArgumentException(
            $"Exclusion spec must be in format 'ComponentName::Reason'. Got: '{exclusionSpec}'",
            nameof(exclusionSpec));

      return new ComponentExclusion(parts[0], parts[1]);
   }

   /// <summary>
   /// Determines if this exclusion matches the given permutation.
   /// </summary>
   public bool Matches(ComponentsPermutation permutation) =>
      permutation.Components.Contains(ComponentName);
}
