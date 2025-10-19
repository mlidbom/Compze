using System.Linq;
using System.Transactions;
using Compze.Tessaging.Hosting;
using Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;
using Compze.Tests.Infrastructure.XUnit.PluggableComponents;
using Compze.Utilities.Threading.Testing;
using FluentAssertions;

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;

public class Transaction_policies : EndpointHostTestBase
{
   [PCT] public void Command_handler_runs_in_transaction_with_isolation_level_Serializable()
   {
      RemoteEndpoint.ExecuteServerRequestInTransaction(session => session.Send(new MyExactlyOnceCommand()));

      var transaction = MyExactlyOnceCommandHandlerThreadGate.AwaitPassedThroughCountEqualTo(1)
                                                .PassedThrough.Single().Transaction;
      transaction.Should().NotBeNull();
      transaction.IsolationLevel.Should().Be(IsolationLevel.Serializable);
   }

   [PCT] public void Command_handler_with_result_runs_in_transaction_with_isolation_level_Serializable()
   {
      var commandResult = ClientEndpoint.ExecuteClientRequest(navigator => navigator.Post(MyAtMostOnceCommandWithResult.Create()));

      commandResult.Should().NotBe(null);

      var transaction = CommandHandlerWithResultThreadGate.AwaitPassedThroughCountEqualTo(1)
                                                          .PassedThrough.Single().Transaction;
      transaction.Should().NotBeNull();
      transaction.IsolationLevel.Should().Be(IsolationLevel.Serializable);
   }

   [PCT] public void Event_handler_runs_in_transaction_with_isolation_level_Serializable()
   {
      ClientEndpoint.ExecuteClientRequest(session => session.Post(MyCreateAggregateCommand.Create()));

      var transaction = MyRemoteAggregateEventHandlerThreadGate.AwaitPassedThroughCountEqualTo(1)
                                                               .PassedThrough.Single().Transaction;
      transaction.Should().NotBeNull();
      transaction.IsolationLevel.Should().Be(IsolationLevel.Serializable);
   }

   [PCT] public void Query_handler_does_not_run_in_transaction()
   {
      ClientEndpoint.ExecuteClientRequest(session => session.Get(new MyQuery()));

      QueryHandlerThreadGate.AwaitPassedThroughCountEqualTo(1)
                            .PassedThrough.Single().Transaction.Should().Be(null);
   }
}