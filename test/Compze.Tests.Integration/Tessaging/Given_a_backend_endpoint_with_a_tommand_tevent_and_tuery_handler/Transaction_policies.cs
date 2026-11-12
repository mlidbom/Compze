using Compze.DependencyInjection;
using System.Transactions;
using Compze.Contracts;
using Compze.Tests.Common.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Must;
using Compze.Tessaging.TessageBus;

namespace Compze.Tests.Integration.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

public class Transaction_policies : EndpointHostTestBase
{
   [PCT] public async Task Tommand_handler_runs_in_transaction_with_isolation_level_ReadCommitted()
   {
      await RemoteEndpoint.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(new MyExactlyOnceTommand());

      await MyExactlyOnceTommandHandlerThreadGate.AwaitPassedThroughCountEqualToAsync(1);
      var transaction = MyExactlyOnceTommandHandlerThreadGate.PassedThrough.Single().Transaction;
      transaction.Must().NotBeNull()
                 .Actual.IsolationLevel
                 .Must().Be(IsolationLevel.ReadCommitted);
   }

   [PCT] public async Task Tommand_handler_with_result_runs_in_transaction_with_isolation_level_ReadCommitted()
   {
      var tommandResult = await Navigator.PostAsync(MyAtMostOnceTypermediaTommandWithResult.Create());

      tommandResult.Must().NotBeNull();

      await TommandHandlerWithResultThreadGate.AwaitPassedThroughCountEqualToAsync(1);
      var transaction = TommandHandlerWithResultThreadGate.PassedThrough.Single().Transaction._assert().NotNull();
      transaction.Must().NotBeNull()
                 .Actual.IsolationLevel
                 .Must().Be(IsolationLevel.ReadCommitted);
   }

   [PCT] public async Task Tevent_handler_runs_in_transaction_with_isolation_level_ReadCommitted()
   {
      await Navigator.PostAsync(MyCreateTaggregateTommand.Create());

      await MyRemoteTaggregateTeventHandlerThreadGate.AwaitPassedThroughCountEqualToAsync(1);
      var transaction = MyRemoteTaggregateTeventHandlerThreadGate.PassedThrough.Single().Transaction;
      transaction.Must().NotBeNull()
                 .Actual.IsolationLevel
                 .Must().Be(IsolationLevel.ReadCommitted);
   }

   [PCT] public async Task Tuery_handler_does_not_run_in_transaction()
   {
      await Navigator.GetAsync(new MyTuery());

      await TueryHandlerThreadGate.AwaitPassedThroughCountEqualToAsync(1);
      TueryHandlerThreadGate.PassedThrough.Single().Transaction.Must().BeNull();
   }
}
