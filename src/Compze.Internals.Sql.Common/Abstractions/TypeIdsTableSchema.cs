namespace Compze.Internals.Sql.Common.Abstractions;

/// <summary>Column and table names for the per-database <c>TypeIds</c> interning table, shared by all engines.</summary>
public static class TypeIdsTableSchema
{
   public const string TableName = "TypeIds";
   public const string Id = nameof(Id);
   public const string TypeString = nameof(TypeString);
}
