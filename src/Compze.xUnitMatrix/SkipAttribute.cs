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
   public TDimension[] Values { get; }
   public string Reason { get; }

#pragma warning disable CA1019 // Single-value constructor argument is absorbed into the Values array property; exposing a redundant Value property would duplicate Values[0].
   public SkipAttribute(TDimension value, string reason)
#pragma warning restore CA1019
   {
      Values = [value];
      Reason = reason;
   }

   public SkipAttribute(TDimension[] values, string reason)
   {
      Values = values;
      Reason = reason;
   }
}
