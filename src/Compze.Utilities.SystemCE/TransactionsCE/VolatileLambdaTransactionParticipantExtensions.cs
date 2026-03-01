using System;
using System.Collections.Generic;
using System.Transactions;
using Compze.Utilities.SystemCE.CollectionsCE.GenericCE;
using Compze.Threading.ResourceAccess;

namespace Compze.Utilities.SystemCE.TransactionsCE;

internal static class VolatileLambdaTransactionParticipantExtensions
{
   static readonly IThreadShared<Dictionary<string, VolatileLambdaTransactionParticipant>> Participants = IThreadShared.WithDefaultTimeouts<Dictionary<string, VolatileLambdaTransactionParticipant>>();

   public static Transaction AddCommitTasks(this Transaction @this, params Action[] actions) => UseParticipant(@this, part => part.AddCommitTasks(actions));
   public static Transaction AddPrepareTasks(this Transaction @this, params Action[] actions) => UseParticipant(@this, part => part.AddPrepareTasks(actions));

   static Transaction UseParticipant(Transaction @this, Action<VolatileLambdaTransactionParticipant> action)
   {
      Participants.Locked(participants =>
      {
         var participant = participants.GetOrAdd(@this.TransactionInformation.LocalIdentifier,
                                                 () =>
                                                 {
                                                    var createdParticipant = new VolatileLambdaTransactionParticipant(
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