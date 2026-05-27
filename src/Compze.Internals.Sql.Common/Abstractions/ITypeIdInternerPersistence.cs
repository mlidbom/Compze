namespace Compze.Internals.Sql.Common.Abstractions;

/// <summary>
/// Engine-specific persistence backing a <see cref="ITypeIdInterner"/>: the <c>TypeIds</c> table operations.
/// The interner invokes every method from inside a suppressed ambient transaction, so implementations must not
/// open their own transactions or assume any ambient one.
/// </summary>
public interface ITypeIdInternerPersistence
{
   /// <summary>True if callers must suppress the ambient transaction before all calls.</summary>
   bool SuppressAmbientTransactionBeforeAllCalls { get; }

   /// <summary>Ensures the <c>TypeIds</c> table exists.</summary>
   void EnsureInitialized();

   /// <summary>All currently persisted mappings, for warming the in-memory cache.</summary>
   IEnumerable<(int Id, string TypeString)> LoadAll();

   /// <summary>Atomically inserts <paramref name="typeString"/> if absent and returns its id (existing or new).</summary>
   int InsertOrGet(string typeString);

   /// <summary>Returns the id for <paramref name="typeString"/> if it is already persisted, otherwise <c>null</c>. Does not insert.</summary>
   int? TryGetId(string typeString);

   /// <summary>Returns the type string for <paramref name="id"/> if present, otherwise <c>null</c>.</summary>
   string? GetById(int id);
}
