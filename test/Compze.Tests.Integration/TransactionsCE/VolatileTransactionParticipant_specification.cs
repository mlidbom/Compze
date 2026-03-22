using System.Transactions;
using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.DependencyInjection;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Must;
using Compze.Tests.Common.Sql.DocumentDb;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.xUnitMatrix;

namespace Compze.Tests.Integration.TransactionsCE;

public class VolatileTransactionParticipant_specification : DocumentDbTestsBase
{
   public class When_a_participant_with_EnlistDuringPrepareRequired_throws_during_OnPrepare : VolatileTransactionParticipant_specification
   {
      [PCT]
      public void data_written_by_another_participant_in_the_same_transaction_is_rolled_back()
      {
         var user = new User { Email = "test@test.com", Password = "password" };

         var failingParticipant = new VolatileLambdaTransactionParticipant(
            enlistmentOptions: EnlistmentOptions.EnlistDuringPrepareRequired,
            onPrepare: () => throw new InvalidOperationException("Simulated OnPrepare failure"));

         Exception? caughtException = null;
         try
         {
            Container.ExecuteTransactionInIsolatedScope(scope =>
            {
               scope.DocumentDbUpdater().Save(user.Id, user);
               failingParticipant.EnsureEnlistedInAnyAmbientTransaction();
            });
         }
#pragma warning disable CA1031
         catch(Exception ex)
#pragma warning restore CA1031
         {
            caughtException = ex;
         }

         caughtException.Must().NotBeNull();

         UseInScope(reader =>
            reader.TryGet(user.Id, out User? _).Must().BeFalse());
      }
   }

   public class When_an_OnCommittedSuccessfully_callback_throws : VolatileTransactionParticipant_specification
   {
      [PCT]
      [Skip<SqlLayer>(SqlLayer.PgSql, "PgSql has a bug where it swallows exceptions thrown by callbacks registered to TransactionCompleted")]
      public void the_original_exception_propagates_not_a_ForceRollback_error()
      {
         var user = new User { Email = "test@test.com", Password = "password" };

         Exception? caughtException = null;
         try
         {
            Container.ExecuteTransactionInIsolatedScope(scope =>
            {
               scope.DocumentDbUpdater().Save(user.Id, user);
               Transaction.Current!.OnCommittedSuccessfully(
                  () => throw new InvalidOperationException("Callback failure after commit"));
            });
         }
#pragma warning disable CA1031
         catch(Exception ex)
#pragma warning restore CA1031
         {
            caughtException = ex;
         }

         caughtException.Must().NotBeNull();
         caughtException.Must().BeAssignableTo<InvalidOperationException>();
         caughtException!.Message.Must().Be("Callback failure after commit");
      }

      [PCT]
      public void data_written_in_the_transaction_is_committed()
      {
         var user = new User { Email = "test@test.com", Password = "password" };

         try
         {
            Container.ExecuteTransactionInIsolatedScope(scope =>
            {
               scope.DocumentDbUpdater().Save(user.Id, user);
               Transaction.Current!.OnCommittedSuccessfully(
                  () => throw new InvalidOperationException("Callback failure after commit"));
            });
         }
#pragma warning disable CA1031
         catch
#pragma warning restore CA1031
         {
            // Expected — the callback throws after commit
         }

         UseInScope(reader =>
            reader.TryGet(user.Id, out User? _).Must().BeTrue());
      }
   }
}
