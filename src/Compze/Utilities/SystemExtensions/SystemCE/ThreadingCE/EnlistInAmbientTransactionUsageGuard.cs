using System;
using System.Transactions;
using Compze.Utilities.SystemCE.TransactionsCE;

namespace Compze.Utilities.SystemCE.ThreadingCE;

class EnlistInAmbientTransactionUsageGuard : IUsageGuard
{
   readonly VolatileLambdaTransactionParticipant _transactionParticipant;

   public EnlistInAmbientTransactionUsageGuard(Action flushCallback)
      => _transactionParticipant = new(EnlistmentOptions.EnlistDuringPrepareRequired, onPrepare: flushCallback);

   public void AssertUseValid() => _transactionParticipant.EnsureEnlistedInAnyAmbientTransaction();
}
