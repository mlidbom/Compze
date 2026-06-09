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
   internal TDimension[] Values { get; }
   internal string Reason { get; }

#pragma warning disable CA1019 // Attribute data is read only inside this library (Values/Reason are internal), so no public accessors are exposed.
   public SkipAttribute(TDimension value, string reason)
   {
      Values = [value];
      Reason = reason;
   }

   public SkipAttribute(TDimension[] values, string reason)
   {
      Values = values;
      Reason = reason;
   }
#pragma warning restore CA1019
}
