using System;
using System.Collections.Generic;
using System.Linq;

namespace Compze.Utilities.Testing.XUnit.ComponentCombinations;

#pragma warning disable CA1711 // Name accurately describes a collection of skip specifications
public class SkipComponentSpecificationsCollection
#pragma warning restore CA1711
{
   readonly IReadOnlyList<SkipComponentSpecification> _skippedComponents;

   SkipComponentSpecificationsCollection(IReadOnlyList<SkipComponentSpecification> skippedComponents) => _skippedComponents = skippedComponents;

   public static SkipComponentSpecificationsCollection FromComponentsAndReasons(IReadOnlyList<Enum> components, string[] reasons)
   {
      if(components.Count != reasons.Length)
         throw new ArgumentException("Number of components must match number of reasons");

      return new SkipComponentSpecificationsCollection(components
                                                      .Select((component, index) => new SkipComponentSpecification(component, reasons[index]))
                                                      .ToList());
   }

   /// <summary>Finds the first skipped component that matches the given combination, if any.</summary>
   public SkipComponentSpecification? SkippedComponentFor(ComponentCombination combination) =>
      _skippedComponents.FirstOrDefault(exclusion => exclusion.Skips(combination));
}
