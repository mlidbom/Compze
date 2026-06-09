namespace Compze.xUnitMatrix;

/// <summary>Represents a single dimension value that is skipped, together with the reason it is skipped.</summary>
class DimensionValueSkipSpecification
{
   readonly Enum _dimensionValue;
   readonly string _reason;

   internal DimensionValueSkipSpecification(Enum dimensionValue, string reason)
   {
      _dimensionValue = dimensionValue ?? throw new ArgumentNullException(nameof(dimensionValue));
      _reason = reason ?? throw new ArgumentNullException(nameof(reason));

      if(string.IsNullOrWhiteSpace(reason))
         throw new ArgumentException("Reason cannot be empty", nameof(reason));
   }

   internal Type DimensionEnumType => _dimensionValue.GetType();
   internal Enum DimensionValue => _dimensionValue;

   internal bool Skips(MatrixCombination combination) =>
      combination.DimensionValues.Any(it => it.Equals(_dimensionValue));

   public override string ToString() => $"{_dimensionValue}: {_reason}";
}
