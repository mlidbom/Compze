using Compze.Internals.Sql.Common.Abstractions;

namespace Compze.Internals.Sql.Common;

/// <summary>
/// <see cref="TypeIdInterner"/> for single-writer providers (SQLite) where a suppressed insert on a second
/// connection would deadlock against the business transaction's writer lock. Persistence joins the ambient
/// transaction, so a mapping commits or rolls back atomically with the document/event that references it.
/// Because a write may roll back, the <c>string → id</c> direction is never cached — writes re-confirm the
/// row each time — while the immutable <c>id → string</c> direction is cached.
/// </summary>
sealed class AmbientTransactionTypeIdInterner(ITypeIdInternerPersistence persistence) : TypeIdInterner(persistence)
{
   public override int GetOrInternId(string canonicalTypeString)
   {
      EnsureLoaded();
      var id = Persistence.InsertOrGet(canonicalTypeString);
      CacheMapping(id, canonicalTypeString);
      return id;
   }

   protected override bool TryGetId(string canonicalTypeString, out int id)
   {
      EnsureLoaded();
      if(Persistence.TryGetId(canonicalTypeString) is { } dbId)
      {
         CacheMapping(dbId, canonicalTypeString);
         id = dbId;
         return true;
      }

      id = 0;
      return false;
   }

   protected override T RunInPersistenceTransaction<T>(Func<T> persistenceOperation) => persistenceOperation();

   protected override void RunInPersistenceTransaction(Action persistenceOperation) => persistenceOperation();
}
