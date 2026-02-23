using System;
using System.Transactions;
using Compze.Utilities.Contracts.UsageGuards;
using Compze.Utilities.SystemCE.UsageGuards;

namespace Compze.Utilities.SystemCE.TransactionsCE;

public class EnlistInAmbientTransactionUsageGuard : IUsageGuard
{
   readonly VolatileLambdaTransactionParticipant _transactionParticipant;

   public EnlistInAmbientTransactionUsageGuard(Action flushCallback)
      => _transactionParticipant = new VolatileLambdaTransactionParticipant(EnlistmentOptions.EnlistDuringPrepareRequired, onPrepare: flushCallback);

   public void EnsureAccessValid() => _transactionParticipant.EnsureEnlistedInAnyAmbientTransaction();
}
