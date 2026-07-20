using Compze.DependencyInjection;
using System.Transactions;
using Compze.Contracts;
using Compze.Tests.Common.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Must;
using Compze.Tessaging.Abstractions.TessageBus;

namespace Compze.Tests.Integration.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

public class Transaction_policies : EndpointHostTestBase
{
   [PCT] public async Task Tommand_handler_runs_in_transaction_with_isolation_level_ReadCommitted()
   {
      await RemoteEndpoint.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(new MyExactlyOnceTommand());

      var transaction = MyExactlyOnceTommandHandlerThreadGate.AwaitPassedThroughCountEqualTo(1)
                                                             .PassedThrough.Single().Transaction;
      transaction.Must().NotBeNull()
                 .Actual.IsolationLevel
                 .Must().Be(IsolationLevel.ReadCommitted);
   }

   [PCT] public void Tommand_handler_with_result_runs_in_transaction_with_isolation_level_ReadCommitted()
   {
      var tommandResult = Navigator.Post(MyAtMostOnceTypermediaTommandWithResult.Create());

      tommandResult.Must().NotBeNull();

      var transaction = TommandHandlerWithResultThreadGate.AwaitPassedThroughCountEqualTo(1)
                                                          .PassedThrough.Single().Transaction._assert().NotNull();
      transaction.Must().NotBeNull()
                 .Actual.IsolationLevel
                 .Must().Be(IsolationLevel.ReadCommitted);
   }

   [PCT] public void Tevent_handler_runs_in_transaction_with_isolation_level_ReadCommitted()
   {
      Navigator.Post(MyCreateTaggregateTommand.Create());

      var transaction = MyRemoteTaggregateTeventHandlerThreadGate.AwaitPassedThroughCountEqualTo(1)
                                                                 .PassedThrough.Single().Transaction;
      transaction.Must().NotBeNull()
                 .Actual.IsolationLevel
                 .Must().Be(IsolationLevel.ReadCommitted);
   }

   [PCT] public void Tuery_handler_does_not_run_in_transaction()
   {
      Navigator.Get(new MyTuery());

      TueryHandlerThreadGate.AwaitPassedThroughCountEqualTo(1)
                            .PassedThrough.Single().Transaction.Must().BeNull();
   }
}
