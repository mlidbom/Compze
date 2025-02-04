﻿using System;
using System.Collections.Generic;
using System.Transactions;
using Compze.SystemCE.CollectionsCE.GenericCE;
using Compze.SystemCE.ThreadingCE.ResourceAccess;

namespace Compze.SystemCE.TransactionsCE;

static class VolatileLambdaTransactionParticipantExtensions
{
   static readonly IThreadShared<Dictionary<string, VolatileLambdaTransactionParticipant>> Participants = ThreadShared.WithDefaultTimeout<Dictionary<string, VolatileLambdaTransactionParticipant>>();

   public static Transaction AddCommitTasks(this Transaction @this, params Action[] actions) => UseParticipant(@this, part => part.AddCommitTasks(actions));
   public static Transaction AddPrepareTasks(this Transaction @this, params Action[] actions) => UseParticipant(@this, part => part.AddPrepareTasks(actions));

   static Transaction UseParticipant(Transaction @this, Action<VolatileLambdaTransactionParticipant> action)
   {
      Participants.Update(participants =>
      {
         var participant = participants.GetOrAdd(@this.TransactionInformation.LocalIdentifier,
                                                 () =>
                                                 {
                                                    var createdParticipant = new VolatileLambdaTransactionParticipant(
                                                       onCommit: () => Participants.Update(parts => parts.Remove(@this.TransactionInformation.LocalIdentifier)),
                                                       onRollback: () => Participants.Update(parts => parts.Remove(@this.TransactionInformation.LocalIdentifier)));
                                                    createdParticipant.EnsureEnlistedInAnyAmbientTransaction();
                                                    return createdParticipant;
                                                 });

         action(participant);
      });
      return @this;
   }
}