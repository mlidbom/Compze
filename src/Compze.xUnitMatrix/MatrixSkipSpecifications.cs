namespace Compze.xUnitMatrix;

#pragma warning disable CA1711 // Name accurately describes a collection of skip specifications
class MatrixSkipSpecification
#pragma warning restore CA1711
{
   readonly IReadOnlyList<ComponentSkipSpecification> _skippedComponents;

   internal MatrixSkipSpecification(IReadOnlyList<ComponentSkipSpecification> skippedComponents) => _skippedComponents = skippedComponents;

   /// <summary>Finds the first skipped component that matches the given combination, if any.</summary>
   internal ComponentSkipSpecification? SkippedComponentFor(MatrixCombination combination) =>
      _skippedComponents.FirstOrDefault(exclusion => exclusion.Skips(combination));
}
