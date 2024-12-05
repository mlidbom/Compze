using System.Threading.Tasks;
using Compze.Messaging;
using Compze.Messaging.Buses;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Compze.SystemCE.TransactionsCE;
using FluentAssertions;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace Compze.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;

public class Transaction_policies_internal : Fixture
{
   [Test] public async Task Calling_PostRemoteAsync_within_a_transaction_with_AtLeastOnceCommand_throws_TransactionPolicyViolationException() =>
      await Invoking(() => TransactionScopeCe.Execute(() => ClientEndpoint.ExecuteClientRequest(session => session.PostAsync(MyAtMostOnceCommandWithResult.Create()))))
           .Should().ThrowAsync<MessageInspector.TransactionPolicyViolationException>();

   [Test] public async Task Calling_GetRemoteAsync_within_a_transaction_with_Query_throws_TransactionPolicyViolationException() =>
      await Invoking(() => TransactionScopeCe.Execute(() => ClientEndpoint.ExecuteClientRequest(session => session.GetAsync(new MyQuery()))))
           .Should().ThrowAsync<MessageInspector.TransactionPolicyViolationException>();

   [Test] public void Calling_PostRemoteAsync_within_a_transaction_AtLeastOnceCommand_throws_TransactionPolicyViolationException() =>
      Invoking(() => TransactionScopeCe.Execute(() => ClientEndpoint.ExecuteClientRequest(session => session.Post(MyAtMostOnceCommandWithResult.Create()))))
        .Should().Throw<MessageInspector.TransactionPolicyViolationException>();

   [Test] public void Calling_PostRemoteAsync_without_a_transaction_with_ExactlyOnceCommand_throws_TransactionPolicyViolationException() =>
      Invoking(() => RemoteEndpoint.ExecuteServerRequest(session => session.Send(new MyExactlyOnceCommand())))
        .Should().Throw<MessageInspector.TransactionPolicyViolationException>();

   [Test] public void Calling_GetRemote_within_a_transaction_with_Query_throws_TransactionPolicyViolationException() =>
      Invoking(() => TransactionScopeCe.Execute(() => ClientEndpoint.ExecuteClientRequest(session => session.Get(new MyQuery()))))
        .Should().Throw<MessageInspector.TransactionPolicyViolationException>();

   public Transaction_policies_internal(string _) : base(_) {}
}
