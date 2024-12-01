using System;
using System.Collections.Generic;
using System.Transactions;
using Composable.Contracts;
using Composable.Functional;

namespace Composable.SystemCE.TransactionsCE;

class VolatileLambdaTransactionParticipant : VolatileTransactionParticipant
{
   readonly Action? _onEnlist;
   readonly Action<TransactionStatus>? _onTransactionCompleted;
   readonly List<Action> _rollbackTasks = [];
   readonly List<Action> _commitTasks = [];
   readonly List<Action> _prepareTasks = [];

   internal VolatileLambdaTransactionParticipant(EnlistmentOptions enlistmentOptions = EnlistmentOptions.None,
                                                 Action? onPrepare = null,
                                                 Action? onCommit = null,
                                                 Action? onRollback = null,
                                                 Action? onEnlist = null,
                                                 Action<TransactionStatus>? onTransactionCompleted = null) : base(enlistmentOptions)
   {
      _onEnlist = onEnlist;
      _onTransactionCompleted = onTransactionCompleted;

      if(onPrepare != null) AddPrepareTasks(onPrepare);
      if(onCommit != null) AddCommitTasks(onCommit);
      if(onRollback != null) AddRollbackTasks(onRollback);
   }

   internal VolatileLambdaTransactionParticipant AddCommitTasks(params Action[] tasks) =>
      this.mutate(_ => _commitTasks.AddRange(tasks));

   internal VolatileLambdaTransactionParticipant AddPrepareTasks(params Action[] tasks) =>
      this.mutate(_ => _prepareTasks.AddRange(tasks));

   internal VolatileLambdaTransactionParticipant AddRollbackTasks(params Action[] tasks) =>
      this.mutate(_ => _rollbackTasks.AddRange(tasks));

   protected override void OnCommit() => _commitTasks.InvokeAll();
   protected override void OnRollback() => _rollbackTasks.InvokeAll();
   protected override void OnPrepare() => _prepareTasks.InvokeAll();
   protected override void OnEnlist()
   {
      base.OnEnlist();
      _onEnlist?.Invoke();
      Transaction.Current!.TransactionCompleted += (_, parameters) =>
      {
         Assert.Argument.NotNull(parameters.Transaction);
         _onTransactionCompleted?.Invoke(parameters.Transaction.TransactionInformation.Status);
      };
   }
}