using System;
using System.Transactions;
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

namespace Compze.Utilities.SystemCE.TransactionsCE;

///<summary>Getting the code for participating in a transaction right is surprisingly tricky and the failures very hard to diagnose.
/// Use this class for all our transaction participants so we only have to get it right once.</summary>
#pragma warning disable CA1033 // Abstract class with explicit interface implementation Motivation: Nothing but the .NET transaction machinery should ever call these methods, and they are given the instance typed as the interface
public abstract class VolatileTransactionParticipant : IEnlistmentNotification
{
   readonly EnlistmentOptions _enlistmentOptions;
   protected VolatileTransactionParticipant(EnlistmentOptions enlistmentOptions = EnlistmentOptions.None) => _enlistmentOptions = enlistmentOptions;

   protected abstract void OnCommit();
   protected abstract void OnRollback();
   protected abstract void OnPrepare();

   protected virtual void OnEnlist() {}

   protected virtual void OnInDoubt() {}

   readonly IMonitor _monitor = IMonitor.WithTimeout(30.Seconds());
   Transaction? _enlistedIn;
   public void EnsureEnlistedInAnyAmbientTransaction() => _monitor.Locked(() =>
   {
      var ambientTransaction = Transaction.Current;
      if(ambientTransaction == null) return;

      if(_enlistedIn == null)
      {
         _enlistedIn = ambientTransaction;
         OnEnlist();
         ambientTransaction.EnlistVolatile(this, _enlistmentOptions);
      } else if(_enlistedIn != ambientTransaction)
      {
         throw new Exception($"Somehow switched to a new transaction. Original: {_enlistedIn.TransactionInformation.LocalIdentifier} new: {ambientTransaction.TransactionInformation.LocalIdentifier}");
      }
   });

   void IEnlistmentNotification.Prepare(PreparingEnlistment preparingEnlistment)
   {
      try
      {
         if(_enlistmentOptions.HasFlag(EnlistmentOptions.EnlistDuringPrepareRequired))
         {
            using var transactionScope = new TransactionScope(_enlistedIn!);
            OnPrepare();
            transactionScope.Complete();
         } else
         {
            OnPrepare();
         }

         preparingEnlistment.Prepared();
      }
#pragma warning disable CA1031 //This is the proper handling of exceptions in the context of IEnlistmentNotification 
      catch(Exception exception)
      {
#pragma warning restore CA1031 //This is the proper handling of exceptions in the context of IEnlistmentNotification
         preparingEnlistment.ForceRollback(exception);
      }
   }

   void IEnlistmentNotification.Commit(Enlistment enlistment)
   {
      OnCommit();
      enlistment.Done();
      _enlistedIn = null;
   }

   void IEnlistmentNotification.Rollback(Enlistment enlistment)
   {
      OnRollback();
      enlistment.Done();
      _enlistedIn = null;
   }

   void IEnlistmentNotification.InDoubt(Enlistment enlistment)
   {
      OnInDoubt();
      enlistment.Done();
      _enlistedIn = null;
   }
}