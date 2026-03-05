using System.Transactions;
using Compze.Internals.SystemCE.UsageGuards;

namespace Compze.Internals.SystemCE.TransactionsCE;

public class EnlistInAmbientTransactionUsageGuard(Action flushCallback) : IUsageGuard
{
   readonly VolatileLambdaTransactionParticipant _transactionParticipant = new(EnlistmentOptions.EnlistDuringPrepareRequired, onPrepare: flushCallback);

   public void EnsureAccessValid() => _transactionParticipant.EnsureEnlistedInAnyAmbientTransaction();
}
