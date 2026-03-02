using System.Linq;
using System.Transactions;
using Compze.Contracts;
using Compze.Tessaging.Hosting;
using Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading.Testing;
using Compze.Utilities.Testing.Must;

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

public class Transaction_policies : EndpointHostTestBase
{
   [PCT] public void Tommand_handler_runs_in_transaction_with_isolation_level_Serializable()
   {
      RemoteEndpoint.ExecuteServerRequestInTransaction(session => session.Send(new MyExactlyOnceTommand()));

      var transaction = MyExactlyOnceTommandHandlerThreadGate.AwaitPassedThroughCountEqualTo(1)
                                                             .PassedThrough.Single().Transaction;
      transaction.Must().NotBeNull()
                 .Actual.IsolationLevel
                 .Must().Be(IsolationLevel.Serializable);
   }

   [PCT] public void Tommand_handler_with_result_runs_in_transaction_with_isolation_level_Serializable()
   {
      var tommandResult = Client.ExecuteRequest(navigator => navigator.Post(MyAtMostOnceTypermediaTommandWithResult.Create()));

      tommandResult.Must().NotBeNull();

      var transaction = TommandHandlerWithResultThreadGate.AwaitPassedThroughCountEqualTo(1)
                                                          .PassedThrough.Single().Transaction._assert().NotNull();
      transaction.Must().NotBeNull()
                 .Actual.IsolationLevel
                 .Must().Be(IsolationLevel.Serializable);
   }

   [PCT] public void Tevent_handler_runs_in_transaction_with_isolation_level_Serializable()
   {
      Client.ExecuteRequest(session => session.Post(MyCreateTaggregateTommand.Create()));

      var transaction = MyRemoteTaggregateTeventHandlerThreadGate.AwaitPassedThroughCountEqualTo(1)
                                                                 .PassedThrough.Single().Transaction;
      transaction.Must().NotBeNull()
                 .Actual.IsolationLevel
                 .Must().Be(IsolationLevel.Serializable);
   }

   [PCT] public void Tuery_handler_does_not_run_in_transaction()
   {
      Client.ExecuteRequest(session => session.Get(new MyTuery()));

      TueryHandlerThreadGate.AwaitPassedThroughCountEqualTo(1)
                            .PassedThrough.Single().Transaction.Must().BeNull();
   }
}
