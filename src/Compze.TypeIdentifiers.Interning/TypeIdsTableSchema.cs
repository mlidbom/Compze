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
   /// <summary>The <c>TypeIds</c> table: one row per conceptual type.</summary>
   public static class Types
   {
      /// <summary>The table name: <c>TypeIds</c>.</summary>
      public const string TableName = "TypeIds";
      /// <summary>The database-local id that storage rows reference in place of a type.</summary>
      public const string Id = nameof(Id);
      /// <summary>The type's current fully-qualified name.</summary>
      public const string CurrentName = nameof(CurrentName);
   }

   /// <summary>The <c>TypeStrings</c> table: every persisted <c>$type</c> spelling, each pointing at its conceptual <see cref="Types.Id"/>.</summary>
   public static class Strings
   {
      /// <summary>The table name: <c>TypeStrings</c>.</summary>
      public const string TableName = "TypeStrings";
      /// <summary>The persisted <c>$type</c> spelling.</summary>
      public const string TypeString = nameof(TypeString);
      /// <summary>The conceptual <see cref="Types.Id"/> this spelling resolves to.</summary>
      public const string TypeId = nameof(TypeId);
      /// <summary>When this spelling was first recorded (UTC).</summary>
      public const string FirstSeenUtc = nameof(FirstSeenUtc);
   }

   /// <summary>The <c>TypeNames</c> table: the append-only history of every fully-qualified name a type has been known by.</summary>
   public static class Names
   {
      /// <summary>The table name: <c>TypeNames</c>.</summary>
      public const string TableName = "TypeNames";
      /// <summary>The conceptual <see cref="Types.Id"/> this name belongs to.</summary>
      public const string TypeId = nameof(TypeId);
      /// <summary>The ordinal of this name within the type's name history.</summary>
      public const string Seq = nameof(Seq);
      /// <summary>A fully-qualified name the type has been known by.</summary>
      public const string FullyQualifiedName = nameof(FullyQualifiedName);
      /// <summary>When this name was first recorded (UTC).</summary>
      public const string FirstSeenUtc = nameof(FirstSeenUtc);
   }
}
