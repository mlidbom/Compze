using System;
using System.Collections.Generic;
using System.Transactions;
using Compze.SystemCE;
using Compze.SystemCE.CollectionsCE.GenericCE;
using Compze.SystemCE.ThreadingCE.ResourceAccess;
using Compze.SystemCE.TransactionsCE;

namespace Compze.Persistence.InMemory.EventStore;

partial class InMemoryEventStorePersistenceLayer
{
   class TransactionLockManager
   {
      readonly Dictionary<Guid, TransactionWideLock> _aggregateGuards = new();

      public TResult WithTransactionWideLock<TResult>(Guid aggregateId, Func<TResult> func) => WithTransactionWideLock(aggregateId, true, func);
      public TResult WithTransactionWideLock<TResult>(Guid aggregateId, bool takeWriteLock, Func<TResult> func)
      {
         if(Transaction.Current != null)
         {
            var @lock = _aggregateGuards.GetOrAdd(aggregateId, () => new TransactionWideLock());
            @lock.AwaitAccess(takeWriteLock);
         }

         return func();
      }

      public void WithTransactionWideLock(Guid aggregateId, Action action) => WithTransactionWideLock(aggregateId, true, action.AsUnitFunc());

      class TransactionWideLock
      {
         public void AwaitAccess(bool takeWriteLock)
         {
            if(OwningTransactionLocalId.Length > 0 && !takeWriteLock)
            {
               return;
            }

            var currentTransactionId = Transaction.Current!.TransactionInformation.LocalIdentifier;
            if(currentTransactionId != OwningTransactionLocalId)
            {
               var @lock = Guard.TakeUpdateLock();
               Transaction.Current.OnCompleted(() =>
               {
                  OwningTransactionLocalId = string.Empty;
                  @lock.Dispose();
               });
               OwningTransactionLocalId = currentTransactionId;
            }
         }

         string OwningTransactionLocalId { get; set; } = string.Empty;
         MonitorCE Guard { get; } = MonitorCE.WithTimeout(1.Minutes());
      }
   }
}