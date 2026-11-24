using System.Transactions;
using Compze.Internals.SystemCE.CollectionsCE.GenericCE;
using Compze.Threading.ResourceAccess;

namespace Compze.Internals.SystemCE.TransactionsCE._private;

static class VolatileLambdaTransactionParticipantExtensions
{
   static readonly IThreadShared<Dictionary<string, VolatileLambdaTransactionParticipant>> Participants = IThreadShared.New(new Dictionary<string, VolatileLambdaTransactionParticipant>());

   public static Transaction AddPrepareTasks(this Transaction @this, params Action[] actions) => UseParticipant(@this, part => part.AddPrepareTasks(actions));

   static Transaction UseParticipant(Transaction @this, Action<VolatileLambdaTransactionParticipant> action)
   {
      Participants.Locked(participants =>
      {
         var participant = participants.GetOrAdd(@this.TransactionInformation.LocalIdentifier,
                                                 () =>
                                                 {
                                                    //At phase 0, so that a prepare task that fails, such as FailOnPrepare's, fails the transaction before any database
                                                    //connection is asked to prepare: Compze's connections commit as the transaction's decision, and refuse a two-phase commit.
                                                    var createdParticipant = new VolatileLambdaTransactionParticipant(
                                                       enlistmentOptions: EnlistmentOptions.EnlistDuringPrepareRequired,
                                                       onCommit: () => Participants.Locked(parts => parts.Remove(@this.TransactionInformation.LocalIdentifier)),
                                                       onRollback: () => Participants.Locked(parts => parts.Remove(@this.TransactionInformation.LocalIdentifier)));
                                                    createdParticipant.EnsureEnlistedInAnyAmbientTransaction();
                                                    return createdParticipant;
                                                 });

         action(participant);
      });
      return @this;
   }
}