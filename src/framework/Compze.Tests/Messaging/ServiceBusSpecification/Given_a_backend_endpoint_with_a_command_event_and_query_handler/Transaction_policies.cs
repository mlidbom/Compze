using System.Linq;
using System.Transactions;
using Compze.Messaging.Buses;
using Compze.Testing.Threading;
using FluentAssertions;
using NUnit.Framework;

namespace Compze.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;

public class Transaction_policies : Fixture
{
   [Test] public void Command_handler_runs_in_transaction_with_isolation_level_Serializable()
   {
      RemoteEndpoint.ExecuteServerRequestInTransaction(session => session.Send(new MyExactlyOnceCommand()));

      var transaction = CommandHandlerThreadGate.AwaitPassedThroughCountEqualTo(1)
                                                .PassedThrough.Single().Transaction;
      transaction.Should().NotBeNull();
      transaction!.IsolationLevel.Should().Be(IsolationLevel.Serializable);
   }

   [Test] public void Command_handler_with_result_runs_in_transaction_with_isolation_level_Serializable()
   {
      var commandResult = ClientEndpoint.ExecuteClientRequest(navigator => navigator.Post(MyAtMostOnceCommandWithResult.Create()));

      commandResult.Should().NotBe(null);

      var transaction = CommandHandlerWithResultThreadGate.AwaitPassedThroughCountEqualTo(1)
                                                          .PassedThrough.Single().Transaction;
      transaction.Should().NotBeNull();
      transaction!.IsolationLevel.Should().Be(IsolationLevel.Serializable);
   }

   [Test] public void Event_handler_runs_in_transaction_with_isolation_level_Serializable()
   {
      ClientEndpoint.ExecuteClientRequest(session => session.Post(MyCreateAggregateCommand.Create()));

      var transaction = MyRemoteAggregateEventHandlerThreadGate.AwaitPassedThroughCountEqualTo(1)
                                                               .PassedThrough.Single().Transaction;
      transaction.Should().NotBeNull();
      transaction!.IsolationLevel.Should().Be(IsolationLevel.Serializable);
   }

   [Test] public void Query_handler_does_not_run_in_transaction()
   {
      ClientEndpoint.ExecuteClientRequest(session => session.Get(new MyQuery()));

      QueryHandlerThreadGate.AwaitPassedThroughCountEqualTo(1)
                            .PassedThrough.Single().Transaction.Should().Be(null);
   }

   public Transaction_policies(string _) : base(_) {}
}