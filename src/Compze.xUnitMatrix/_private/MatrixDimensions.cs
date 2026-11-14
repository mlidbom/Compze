using Compze.Internals.SystemCE.LinqCE;

namespace Compze.xUnitMatrix._private;

///<summary>
/// The dimensions of a test matrix, in the order its matrix attribute declares them. A <see cref="MatrixCombination"/> holds one
/// value of each, in the same order.
///</summary>
sealed class MatrixDimensions : IEquatable<MatrixDimensions>
{
   readonly IReadOnlyList<MatrixDimension> _dimensions;

   MatrixDimensions(IReadOnlyList<MatrixDimension> dimensions) => _dimensions = dimensions;

   /// <summary>
   /// The dimensions whose enum types are <paramref name="dimensionEnumTypes"/>, in that order.<br/>
   /// Throws <see cref="InvalidMatrixConfigurationException"/> when there are none, or one of them is not an enum type.
   /// </summary>
   public static MatrixDimensions OfEnumTypes(IReadOnlyList<Type> dimensionEnumTypes)
   {
      if(dimensionEnumTypes.Count == 0)
         throw new InvalidMatrixConfigurationException("A matrix needs at least one dimension, but its matrix attribute declares no dimension enum types.");

      if(dimensionEnumTypes.FirstOrDefault(it => !it.IsEnum) is {} notAnEnumType)
         throw new InvalidMatrixConfigurationException($"Each dimension of a matrix must be an enum type, but {notAnEnumType.Name} is not.");

      return new MatrixDimensions(dimensionEnumTypes.Select(it => new MatrixDimension(it)).ToList());
   }

   public int Count => _dimensions.Count;

   public MatrixDimension this[int dimensionIndex] => _dimensions[dimensionIndex];

   public bool Contains(Type dimensionEnumType) => _dimensions.Any(it => it.EnumType == dimensionEnumType);

   /// <summary>Every combination of the dimensions' values: their full Cartesian product.</summary>
   public IReadOnlyList<MatrixCombination> AllCombinations =>
      _dimensions.Select(it => it.AllValues)
                 .CartesianProduct()
                 .Select(MatrixCombination.FromDimensionValues)
                 .ToList();

   /// <summary>The dimensions' names, in order and separated as a <see cref="MatrixCombination"/>'s values are, such as <c>PersistenceLayer:DIContainer</c>.</summary>
   public override string ToString() => string.Join(MatrixCombination.Separator, _dimensions);

   public bool Equals(MatrixDimensions? other) => other is not null && _dimensions.SequenceEqual(other._dimensions);

   public override bool Equals(object? obj) => obj is MatrixDimensions other && Equals(other);

   public override int GetHashCode() => _dimensions.Aggregate(0, (hashCode, dimension) => HashCode.Combine(hashCode, dimension));
}
