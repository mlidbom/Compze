namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

internal class ExclusionsCollection
{
   readonly IReadOnlyList<ComponentExclusion> _exclusions;

   ExclusionsCollection(IReadOnlyList<ComponentExclusion> exclusions) => _exclusions = exclusions;

   public static ExclusionsCollection FromComponentsAndReasons(IReadOnlyList<Enum> components, string[] reasons)
   {
      if(components.Count != reasons.Length)
         throw new ArgumentException("Number of components must match number of reasons");

      return new ExclusionsCollection(components
                                     .Select((component, index) => new ComponentExclusion(component, reasons[index]))
                                     .ToList());
   }

   /// <summary>Finds the first exclusion that matches the given permutation, if any.</summary>
   public ComponentExclusion? FindMatchingExclusion(ComponentsPermutation permutation) =>
      _exclusions.FirstOrDefault(exclusion => exclusion.Excludes(permutation));

   /// <summary>Checks if any exclusion matches the given permutation.</summary>
   public bool Matches(ComponentsPermutation permutation) =>
      FindMatchingExclusion(permutation) != null;
}
