using System.Transactions;

namespace Compze.Sql.InternalSpecifications.TransactionCommit;

///<summary>A durable resource of a transaction's own, as a consumer's own resource manager would be: one that can commit in a single<br/>
/// phase, as the transaction's decision, so that it takes part without making the transaction distributed.</summary>
///<remarks>A durable participant that can't commit in a single phase, one that implements only <see cref="IEnlistmentNotification"/>,<br/>
/// makes .NET promote its transaction to a distributed one as it enlists.</remarks>
sealed class DurableResourceOfItsOwn : ISinglePhaseNotification
{
   ///<summary>Has a new one take part in the ambient transaction as a durable resource.</summary>
   internal static void TakesPartInTheAmbientTransaction() =>
      Transaction.Current!.EnlistDurable(Guid.NewGuid(), new DurableResourceOfItsOwn(), EnlistmentOptions.None);

   public void SinglePhaseCommit(SinglePhaseEnlistment singlePhaseEnlistment) => singlePhaseEnlistment.Committed();
   public void Prepare(PreparingEnlistment preparingEnlistment) => preparingEnlistment.Prepared();
   public void Commit(Enlistment enlistment) => enlistment.Done();
   public void Rollback(Enlistment enlistment) => enlistment.Done();
   public void InDoubt(Enlistment enlistment) => enlistment.Done();
}
