using System;
using System.Transactions;
using Compze.Threading;

namespace Compze.Utilities.SystemCE.TransactionsCE;

class EnlistInAmbientTransactionUsageGuard : IUsageGuard
{
   readonly VolatileLambdaTransactionParticipant _transactionParticipant;

   public EnlistInAmbientTransactionUsageGuard(Action flushCallback)
      => _transactionParticipant = new(EnlistmentOptions.EnlistDuringPrepareRequired, onPrepare: flushCallback);

   public void EnsureAccessValid() => _transactionParticipant.EnsureEnlistedInAnyAmbientTransaction();
}
