using System.Collections.Concurrent;
using Compze.Internals.Sql.Common.Abstractions;
using Compze.TypeIdentifiers;

namespace Compze.Internals.Sql.Common;

/// <summary>
/// Engine-agnostic <see cref="ITypeIdInterner"/>: an in-memory bidirectional cache over a per-database
/// <c>TypeIds</c> table, loaded once on first use. <see cref="TypeId"/> is the public currency; the canonical
/// string is its persistence form, so the table (and <see cref="ITypeIdInternerPersistence"/>) stay pure
/// <c>string ↔ int</c> while this class bridges <c>TypeId ↔ int</c> via <see cref="ITypeMap"/>.
/// The transaction strategy is fixed per provider by
/// <see cref="ITypeIdInternerPersistence.SuppressAmbientTransactionBeforeAllCalls"/> and chosen once at
/// construction via <see cref="For"/> — there are two concrete subclasses, one per strategy, so neither
/// re-checks the flag on every call:
/// <list type="bullet">
///   <item><see cref="SuppressedTransactionTypeIdInterner"/> (MVCC engines): persistence runs in a
///   suppressed scope, so a mapping commits immediately and durably regardless of the business
///   transaction. Mappings are cached in both directions permanently; the database is touched only on a
///   cache miss.</item>
///   <item><see cref="AmbientTransactionTypeIdInterner"/> (SQLite): persistence joins the ambient
///   transaction (same connection, no writer-lock conflict), so a mapping commits or rolls back
///   atomically with the document/event that references it. The <c>string → id</c> direction is never
///   cached (a write could roll back, and a re-insert may get a different id), so writes re-confirm the
///   row each time.</item>
/// </list>
/// </summary>
public abstract class TypeIdInterner(ITypeIdInternerPersistence persistence, ITypeMap typeMap) : ITypeIdInterner
{
   protected ITypeIdInternerPersistence Persistence { get; } = persistence;
   readonly ITypeMap _typeMap = typeMap;
   readonly ConcurrentDictionary<int, string> _idToString = new();
   readonly Lock _loadLock = new();
   bool _loaded;

   public static ITypeIdInterner For(ITypeIdInternerPersistence persistence, ITypeMap typeMap) =>
      persistence.SuppressAmbientTransactionBeforeAllCalls
         ? new SuppressedTransactionTypeIdInterner(persistence, typeMap)
         : new AmbientTransactionTypeIdInterner(persistence, typeMap);

   public int GetOrInternId(TypeId typeId) => InternCanonical(typeId.CanonicalString);

   public bool TryGetInternedId(TypeId typeId, out int internedId) => TryGetCanonical(typeId.CanonicalString, out internedId);

   public TypeId GetTypeId(int internedId) => _typeMap.GetIdFromPersistedString(ResolveCanonicalString(internedId));

   /// <summary>Interns the canonical string, assigning and persisting a new id if it has never been seen.</summary>
   protected abstract int InternCanonical(string canonicalTypeString);

   /// <summary>Returns the id for an already-interned canonical string, or <c>false</c> if it was never interned.</summary>
   protected abstract bool TryGetCanonical(string canonicalTypeString, out int id);

   string ResolveCanonicalString(int internedId)
   {
      EnsureLoaded();
      if(_idToString.TryGetValue(internedId, out var typeString))
         return typeString;

      var fromDb = RunInPersistenceTransaction(() => Persistence.GetById(internedId))
                ?? throw new InvalidOperationException($"No interned type string found for id {internedId}.");
      CacheMapping(internedId, fromDb);
      return fromDb;
   }

   /// <summary>Runs every persistence operation in the strategy's transaction context.</summary>
   protected abstract T RunInPersistenceTransaction<T>(Func<T> persistenceOperation);

   protected abstract void RunInPersistenceTransaction(Action persistenceOperation);

   /// <summary>
   /// Caches the always-safe <c>id → string</c> direction. The suppressed subclass overrides this to also
   /// cache <c>string → id</c>, which is only safe when mappings commit independently of the business transaction.
   /// </summary>
   protected virtual void CacheMapping(int id, string canonicalTypeString) => _idToString[id] = canonicalTypeString;

   protected void EnsureLoaded()
   {
      if(Volatile.Read(ref _loaded))
         return;

      lock(_loadLock)
      {
         if(_loaded)
            return;

         // At first use no insert has happened in any ambient transaction yet, so LoadAll reads only committed
         // mappings — safe to cache regardless of the strategy.
         RunInPersistenceTransaction(() =>
         {
            Persistence.EnsureInitialized();
            foreach(var (id, typeString) in Persistence.LoadAll())
               CacheMapping(id, typeString);
         });
         _loaded = true;
      }
   }
}
