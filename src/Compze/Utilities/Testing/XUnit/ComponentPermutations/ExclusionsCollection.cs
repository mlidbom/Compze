namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

/// <summary>
/// A collection of component exclusions with matching logic.
/// </summary>
internal class ExclusionsCollection
{
   readonly IReadOnlyList<ComponentExclusion> _exclusions;

   ExclusionsCollection(IReadOnlyList<ComponentExclusion> exclusions)
   {
      _exclusions = exclusions;
   }

   /// <summary>
   /// Parses exclusion specifications into a collection.
   /// Format: "ComponentName::Reason"
   /// </summary>
   public static ExclusionsCollection Parse(string[] exclusionSpecs)
   {
      var exclusions = exclusionSpecs.Select(ComponentExclusion.Parse).ToList();
      
      // Validate that all excluded components actually exist
      var validComponents = PluggableComponentsReader.Components;
      var invalidComponents = exclusions
         .Select(e => e.ComponentName)
         .Where(name => !validComponents.Contains(name))
         .ToList();

      if(invalidComponents.Count > 0)
      {
         throw new ArgumentException(
            $"The following excluded components do not exist in the configuration: {string.Join(", ", invalidComponents)}. " +
            $"Valid components are: {string.Join(", ", validComponents)}");
      }

      return new ExclusionsCollection(exclusions);
   }

   /// <summary>
   /// Finds the first exclusion that matches the given permutation, if any.
   /// </summary>
   public ComponentExclusion? FindMatchingExclusion(ComponentsPermutation permutation)
   {
      foreach(var exclusion in _exclusions)
      {
         if(exclusion.Matches(permutation))
            return exclusion;
      }

      return null;
   }

   /// <summary>
   /// Checks if any exclusion matches the given permutation.
   /// </summary>
   public bool Matches(ComponentsPermutation permutation) => 
      FindMatchingExclusion(permutation) != null;
}
