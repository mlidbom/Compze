using System;
using System.Transactions;
using Compze.Utilities.Contracts.UsageGuards;

namespace Compze.Utilities.SystemCE.TransactionsCE;

internal class EnlistInAmbientTransactionUsageGuard : IUsageGuard
{
   readonly VolatileLambdaTransactionParticipant _transactionParticipant;

   public EnlistInAmbientTransactionUsageGuard(Action flushCallback)
      => _transactionParticipant = new VolatileLambdaTransactionParticipant(EnlistmentOptions.EnlistDuringPrepareRequired, onPrepare: flushCallback);

   public void EnsureAccessValid() => _transactionParticipant.EnsureEnlistedInAnyAmbientTransaction();
}
