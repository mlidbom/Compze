using System.Collections.Concurrent;
using System.Transactions;
using Compze.Internals.Sql.Common.Abstractions;
using Compze.Internals.SystemCE.TransactionsCE;

namespace Compze.Internals.Sql.Common;

/// <summary>
/// <see cref="TypeIdInterner"/> for providers whose interning commits independently of the business
/// transaction (MVCC engines). Persistence runs in a suppressed scope, so mappings are durable the moment
/// they are written and can be cached permanently in both directions.
/// </summary>
sealed class SuppressedTransactionTypeIdInterner(ITypeIdInternerPersistence persistence) : TypeIdInterner(persistence)
{
   readonly ConcurrentDictionary<string, int> _stringToId = new(StringComparer.Ordinal);

   public override int GetOrInternId(string canonicalTypeString)
   {
      EnsureLoaded();
      if(_stringToId.TryGetValue(canonicalTypeString, out var cached))
         return cached;

      var id = RunInPersistenceTransaction(() => Persistence.InsertOrGet(canonicalTypeString));
      CacheMapping(id, canonicalTypeString);
      return id;
   }

   protected override bool TryGetId(string canonicalTypeString, out int id)
   {
      EnsureLoaded();
      if(_stringToId.TryGetValue(canonicalTypeString, out id))
         return true;

      if(RunInPersistenceTransaction(() => Persistence.TryGetId(canonicalTypeString)) is { } dbId)
      {
         CacheMapping(dbId, canonicalTypeString);
         id = dbId;
         return true;
      }

      id = 0;
      return false;
   }

   protected override void CacheMapping(int id, string canonicalTypeString)
   {
      base.CacheMapping(id, canonicalTypeString);
      _stringToId[canonicalTypeString] = id;
   }

   protected override T RunInPersistenceTransaction<T>(Func<T> persistenceOperation) =>
      TransactionScopeCe.Execute(persistenceOperation, TransactionScopeOption.Suppress);

   protected override void RunInPersistenceTransaction(Action persistenceOperation) =>
      TransactionScopeCe.SuppressAmbient(persistenceOperation);
}
