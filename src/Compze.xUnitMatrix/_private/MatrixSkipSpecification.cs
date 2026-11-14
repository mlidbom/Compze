using System.Reflection;

namespace Compze.xUnitMatrix._private;

///<summary>The dimension values a test method's <see cref="SkipAttribute{TDimension}"/>s skip, each with the reason it is skipped.</summary>
#pragma warning disable CA1711 // Name accurately describes a collection of skip specifications
class MatrixSkipSpecification
#pragma warning restore CA1711
{
   readonly IReadOnlyList<DimensionValueSkipSpecification> _skippedDimensionValues;

   MatrixSkipSpecification(IReadOnlyList<DimensionValueSkipSpecification> skippedDimensionValues) => _skippedDimensionValues = skippedDimensionValues;

   /// <summary>
   /// The dimension values the <see cref="SkipAttribute{TDimension}"/>s on <paramref name="testMethod"/> skip.<br/>
   /// Throws <see cref="InvalidMatrixConfigurationException"/> when one skips a value of a type that is not one of
   /// <paramref name="dimensions"/>, or gives no reason.
   /// </summary>
   internal static MatrixSkipSpecification DeclaredOn(MethodInfo testMethod, MatrixDimensions dimensions)
   {
      var skippedDimensionValues = SkippedDimensionValuesDeclaredOn(testMethod);

      if(skippedDimensionValues.FirstOrDefault(it => !dimensions.Contains(it.DimensionEnumType)) is {} notADimensionValue)
         throw new InvalidMatrixConfigurationException(
            $"{testMethod.DeclaringType?.Name}.{testMethod.Name} skips {notADimensionValue.DimensionValue}, a value of {notADimensionValue.DimensionEnumType.Name}, "
          + $"which is not one of the matrix's dimensions: {dimensions}.");

      return new MatrixSkipSpecification(skippedDimensionValues);
   }

   /// <summary>Finds the first skipped dimension value that matches the given combination, if any.</summary>
   internal DimensionValueSkipSpecification? SkippedDimensionValueFor(MatrixCombination combination) =>
      _skippedDimensionValues.FirstOrDefault(skip => skip.Skips(combination));

   static IReadOnlyList<DimensionValueSkipSpecification> SkippedDimensionValuesDeclaredOn(MethodInfo testMethod)
   {
      var skipAttributeType = typeof(SkipAttribute<>);
      return testMethod.GetCustomAttributes(inherit: true)
                       .Where(attr => attr.GetType().IsGenericType && attr.GetType().GetGenericTypeDefinition() == skipAttributeType)
                       .SelectMany(attr =>
                        {
                           var attrType = attr.GetType();
                           const BindingFlags skipPropertyFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                           var values = (Array)attrType.GetProperty(nameof(SkipAttribute<>.SkippedDimensionValues), skipPropertyFlags)!.GetValue(attr)!;
                           var reason = (string?)attrType.GetProperty(nameof(SkipAttribute<>.Reason), skipPropertyFlags)!.GetValue(attr);
                           return values.Cast<Enum>().Select(value => new DimensionValueSkipSpecification(value, reason));
                        })
                       .ToList();
   }
}
