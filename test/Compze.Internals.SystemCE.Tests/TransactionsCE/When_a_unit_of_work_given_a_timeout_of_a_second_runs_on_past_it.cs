using System.Diagnostics;
using System.Transactions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.Threading;
using Compze.Threading.Testing;

namespace Compze.Internals.SystemCE.Tests.TransactionsCE;

///<summary>A unit of work of <see cref="TransactionScopeCe.ExecuteAsync(Func{Task},TimeSpan,TransactionScopeOption)"/>, given a<br/>
/// timeout of a second, whose action waits, inside its transaction, until the transaction has ended or ten seconds have passed.</summary>
public abstract class When_a_unit_of_work_given_a_timeout_of_a_second_runs_on_past_it : UniversalTestBase
{
   static readonly TimeSpan ASecond = TimeSpan.FromSeconds(1);
   static readonly WaitTimeout HowLongTheActionWaitsForItsTransactionToEnd = WaitTimeout.Seconds(10);

   TransactionStatus _statusOfItsTransactionAsTheActionEnded;
   Exception? _whatTheUnitOfWorkThrew;

   protected TransactionStatus StatusOfItsTransactionAsTheActionEnded => _statusOfItsTransactionAsTheActionEnded;
   protected Exception? WhatTheUnitOfWorkThrew => _whatTheUnitOfWorkThrew;

   ///<summary>Runs the unit of work, with <paramref name="whileItsTransactionIsActive"/> started inside it, before it waits.</summary>
   protected async Task RunTheUnitOfWorkAsync(Func<Task> whileItsTransactionIsActive)
   {
      try
      {
         await TransactionScopeCe.ExecuteAsync(async () =>
         {
            var transaction = Transaction.Current!;
            var theTransactionHasEnded = IThreadGate.NewOpen(HowLongTheActionWaitsForItsTransactionToEnd, "TheTransactionHasEnded");
            transaction.TransactionCompleted += (_, _) => theTransactionHasEnded.AwaitPassThrough();
            var meanwhile = whileItsTransactionIsActive();
            await theTransactionHasEnded.TryAwaitPassedThroughCountEqualToAsync(1, HowLongTheActionWaitsForItsTransactionToEnd);
            await meanwhile;
            _statusOfItsTransactionAsTheActionEnded = transaction.TransactionInformation.Status;
         }, ASecond);
      }
      catch(TransactionAbortedException aborted)
      {
         _whatTheUnitOfWorkThrew = aborted;
      }
   }

   public class whose_action_starts_nothing_else : When_a_unit_of_work_given_a_timeout_of_a_second_runs_on_past_it
   {
      protected override async Task InitializeAsyncInternal() => await RunTheUnitOfWorkAsync(() => Task.CompletedTask);

      [XF] public void its_transaction_is_rolled_back_at_its_timeout() => StatusOfItsTransactionAsTheActionEnded.Must().Be(TransactionStatus.Aborted);
      [XF] public void it_throws_a_TransactionAbortedException_caused_by_a_TimeoutException() => WhatTheUnitOfWorkThrew.Must().Satisfy(it => it is TransactionAbortedException { InnerException: TimeoutException });
   }

   ///<summary>What loses a transaction's timeout in .NET 11.0.0, by dotnet/runtime#136986's reproduction: for a second and a half,<br/>
   /// sixteen threads each start transactions, each with a timeout half a second longer than the last.</summary>
   public class while_sixteen_threads_start_transactions_with_ever_longer_timeouts : When_a_unit_of_work_given_a_timeout_of_a_second_runs_on_past_it
   {
      static readonly TimeSpan HowLongTheThreadsStartTransactions = TimeSpan.FromSeconds(1.5);

      protected override async Task InitializeAsyncInternal() =>
         await RunTheUnitOfWorkAsync(() => Task.WhenAll(1.Through(16).Select(_ => TaskCE.RunOnDedicatedThreadAsync(StartTransactionsWithEverLongerTimeouts))));

      [XF] public void its_transaction_is_rolled_back_at_its_timeout() => StatusOfItsTransactionAsTheActionEnded.Must().Be(TransactionStatus.Aborted);
      [XF] public void it_throws_a_TransactionAbortedException_caused_by_a_TimeoutException() => WhatTheUnitOfWorkThrew.Must().Satisfy(it => it is TransactionAbortedException { InnerException: TimeoutException });

      static void StartTransactionsWithEverLongerTimeouts()
      {
         var clock = Stopwatch.StartNew();
         for(var round = 0; clock.Elapsed < HowLongTheThreadsStartTransactions; round++)
         {
            using var transaction = new CommittableTransaction(TimeSpan.FromSeconds(10) + TimeSpan.FromMilliseconds(512) * round);
         }
      }
   }
}
