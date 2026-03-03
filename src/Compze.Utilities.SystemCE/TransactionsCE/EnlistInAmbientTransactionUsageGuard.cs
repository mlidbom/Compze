using System.Transactions;
using Compze.Utilities.SystemCE.UsageGuards;

namespace Compze.Utilities.SystemCE.TransactionsCE;

public class EnlistInAmbientTransactionUsageGuard(Action flushCallback) : IUsageGuard
{
   readonly VolatileLambdaTransactionParticipant _transactionParticipant = new(EnlistmentOptions.EnlistDuringPrepareRequired, onPrepare: flushCallback);

   public void EnsureAccessValid() => _transactionParticipant.EnsureEnlistedInAnyAmbientTransaction();
}
