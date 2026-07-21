using Compze.Underscore;
using Compze.Internals.SystemCE.LinqCE;

namespace Compze.xUnitMatrix.Private;

class MatrixConfigurationFileLine
{
   const string Wildcard = "*";
   readonly IReadOnlyList<Type> _dimensionEnumTypes;
   readonly IReadOnlyList<string> _dimensionValueNamesOrWildCards;
   readonly IReadOnlyList<WildcardDimension> _wildCardDimensionValues;

   public MatrixConfigurationFileLine(IReadOnlyList<Type> dimensionEnumTypes, string line)
   {
      _dimensionEnumTypes = dimensionEnumTypes;
      _dimensionValueNamesOrWildCards = line.Split(MatrixCombination.Separator);
      _wildCardDimensionValues = _dimensionValueNamesOrWildCards
                           .Zip(_dimensionEnumTypes, (dimensionValueName, dimensionEnumType) => new { dimensionValueName, dimensionEnumType })
                           .Where(it => it.dimensionValueName == Wildcard)
                           .Select(it => new WildcardDimension(it.dimensionEnumType))
                           .ToList();
   }

   public IReadOnlyList<MatrixCombination> ExpandWildcardsIntoConcreteCombinations()
   {
      if(!_wildCardDimensionValues.Any())
         return
         [
            MatrixCombination.FromDimensionValues(_dimensionValueNamesOrWildCards
                                                         .Zip(_dimensionEnumTypes, (name, type) => (Enum)Enum.Parse(type, name))
                                                         .ToList())
         ];

      return _wildCardDimensionValues
            .Select(it => it.AllDimensionValues)
            .CartesianProduct()
            .Select(it => new ResolvedWildcardDimensionValues(it))
            .Select(CreateConcreteCombination)
            .ToList();
   }

   MatrixCombination CreateConcreteCombination(ResolvedWildcardDimensionValues wildCardReplacementValues) =>
      _dimensionValueNamesOrWildCards
        .Select((dimensionValueName, dimensionIndex) =>
                   dimensionValueName == Wildcard
                      ? wildCardReplacementValues.DimensionValueFor(_dimensionEnumTypes[dimensionIndex])
                      : ParseDimensionValue(dimensionIndex, dimensionValueName))
        ._(MatrixCombination.FromDimensionValues);

   Enum ParseDimensionValue(int dimensionIndex, string dimensionValueName) => (Enum)Enum.Parse(_dimensionEnumTypes[dimensionIndex], dimensionValueName);

   readonly record struct WildcardDimension(Type DimensionEnumType)
   {
      public IReadOnlyList<Enum> AllDimensionValues => Enum.GetValues(DimensionEnumType).Cast<Enum>().ToReadOnlyList();
   }

   class ResolvedWildcardDimensionValues(IReadOnlyList<Enum> dimensionValues)
   {
      readonly IReadOnlyList<Enum> _dimensionValues = dimensionValues;
      public Enum DimensionValueFor(Type dimensionEnumType) => _dimensionValues.Single(predicate: it => it.GetType() == dimensionEnumType);
   }
}
