namespace Compze.xUnitMatrix;

#pragma warning disable CA1711 // Name accurately describes a collection of skip specifications
class MatrixSkipSpecification
#pragma warning restore CA1711
{
   readonly IReadOnlyList<DimensionValueSkipSpecification> _skippedDimensionValues;

   internal MatrixSkipSpecification(IReadOnlyList<DimensionValueSkipSpecification> skippedDimensionValues) => _skippedDimensionValues = skippedDimensionValues;

   /// <summary>Finds the first skipped dimension value that matches the given combination, if any.</summary>
   internal DimensionValueSkipSpecification? SkippedDimensionValueFor(MatrixCombination combination) =>
      _skippedDimensionValues.FirstOrDefault(skip => skip.Skips(combination));
}
