namespace Compze.xUnitMatrix;

/// <summary>
/// Skips specific dimension values from a matrix theory test, with a required reason.
/// Uses a generic type parameter to preserve the enum type through IL metadata encoding.
/// Apply multiple times to skip values from different dimensions.
/// </summary>
/// <example>
/// <code>
/// // Single value:
/// [Skip&lt;SqlLayer&gt;(SqlLayer.Sqlite, "SQLite doesn't support this feature")]
///
/// // Multiple values:
/// [Skip&lt;SqlLayer&gt;([SqlLayer.Sqlite, SqlLayer.SqliteMemory], "SQLite doesn't support this feature")]
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class SkipAttribute<TDimension> : Attribute
   where TDimension : struct, Enum
{
   // CA1019 wants a public, one-per-argument property for every positional attribute argument. Neither fits here:
   // the attribute data is read only inside this library (SkippedDimensionValues/Reason are internal), and the single-value
   // constructor folds its argument into the SkippedDimensionValues collection, so there is intentionally no per-argument property.
#pragma warning disable CA1019
   internal TDimension[] SkippedDimensionValues { get; }
   internal string Reason { get; }

   public SkipAttribute(TDimension value, string reason)
   {
      SkippedDimensionValues = [value];
      Reason = reason;
   }

   public SkipAttribute(TDimension[] skippedDimensionValues, string reason)
   {
      SkippedDimensionValues = skippedDimensionValues;
      Reason = reason;
   }
#pragma warning restore CA1019
}
