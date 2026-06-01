namespace Compze.TypeIdentifiers.Interning;

/// <summary>
/// Table and column names for the per-database type-identity tables, shared by all engines.
/// <list type="bullet">
///   <item><see cref="Types"/> — one row per conceptual type: the database-local <c>Id</c> that storage rows
///   reference, plus its current fully-qualified name.</item>
///   <item><see cref="Strings"/> — every persisted <c>$type</c> spelling a type has had, each pointing at its
///   conceptual <c>Id</c>. The spelling column is never indexed and carries no length ceiling.</item>
///   <item><see cref="Names"/> — the append-only history of every fully-qualified name a type has been known by.</item>
/// </list>
/// </summary>
public static class TypeIdsTableSchema
{
   public static class Types
   {
      public const string TableName = "TypeIds";
      public const string Id = nameof(Id);
      public const string CurrentName = nameof(CurrentName);
   }

   public static class Strings
   {
      public const string TableName = "TypeStrings";
      public const string TypeString = nameof(TypeString);
      public const string TypeId = nameof(TypeId);
      public const string FirstSeenUtc = nameof(FirstSeenUtc);
   }

   public static class Names
   {
      public const string TableName = "TypeNames";
      public const string TypeId = nameof(TypeId);
      public const string Seq = nameof(Seq);
      public const string FullyQualifiedName = nameof(FullyQualifiedName);
      public const string FirstSeenUtc = nameof(FirstSeenUtc);
   }
}
