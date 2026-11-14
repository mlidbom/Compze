namespace Compze.xUnitMatrix._private;

/// <summary>Represents a single dimension value that is skipped, together with the reason it is skipped.</summary>
class DimensionValueSkipSpecification
{
   readonly Enum _dimensionValue;
   readonly string _reason;

   /// <summary>Throws <see cref="InvalidMatrixConfigurationException"/> when <paramref name="reason"/> is missing or blank.</summary>
   internal DimensionValueSkipSpecification(Enum dimensionValue, string? reason)
   {
      if(string.IsNullOrWhiteSpace(reason))
         throw new InvalidMatrixConfigurationException($"Skipping {dimensionValue} takes a reason, shown in the test runner, but its {nameof(SkipAttribute<>)} gives none.");

      _dimensionValue = dimensionValue;
      _reason = reason;
   }

   internal Type DimensionEnumType => _dimensionValue.GetType();
   internal Enum DimensionValue => _dimensionValue;

   internal bool Skips(MatrixCombination combination) =>
      combination.DimensionValues.Any(it => it.Equals(_dimensionValue));

   public override string ToString() => $"{_dimensionValue}: {_reason}";
}
