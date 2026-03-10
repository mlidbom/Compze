namespace Compze.xUnitMatrix;

#pragma warning disable CA1711 // Name accurately describes a collection of skip specifications
class MatrixSkipSpecification
#pragma warning restore CA1711
{
   readonly IReadOnlyList<ComponentSkipSpecification> _skippedComponents;

   MatrixSkipSpecification(IReadOnlyList<ComponentSkipSpecification> skippedComponents) => _skippedComponents = skippedComponents;

   internal static MatrixSkipSpecification FromComponentsAndReasons(IReadOnlyList<Enum> components, string[] reasons)
   {
      if(components.Count != reasons.Length)
         throw new ArgumentException("Number of components must match number of reasons");

      return new MatrixSkipSpecification(components
                                                      .Select((component, index) => new ComponentSkipSpecification(component, reasons[index]))
                                                      .ToList());
   }

   /// <summary>Finds the first skipped component that matches the given combination, if any.</summary>
   internal ComponentSkipSpecification? SkippedComponentFor(MatrixCombination combination) =>
      _skippedComponents.FirstOrDefault(exclusion => exclusion.Skips(combination));
}
