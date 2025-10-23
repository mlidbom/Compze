using System;
using System.Collections.Generic;
using System.Linq;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

internal class SkippedComponentsCollection
{
   readonly IReadOnlyList<SkippedComponent> _exclusions;

   SkippedComponentsCollection(IReadOnlyList<SkippedComponent> exclusions) => _exclusions = exclusions;

   public static SkippedComponentsCollection FromComponentsAndReasons(IReadOnlyList<Enum> components, string[] reasons)
   {
      if(components.Count != reasons.Length)
         throw new ArgumentException("Number of components must match number of reasons");

      return new SkippedComponentsCollection(components
                                     .Select((component, index) => new SkippedComponent(component, reasons[index]))
                                     .ToList());
   }

   /// <summary>Finds the first exclusion that matches the given permutation, if any.</summary>
   public SkippedComponent? SkippedComponentFor(ComponentsPermutation permutation) =>
      _exclusions.FirstOrDefault(exclusion => exclusion.Skips(permutation));
}
