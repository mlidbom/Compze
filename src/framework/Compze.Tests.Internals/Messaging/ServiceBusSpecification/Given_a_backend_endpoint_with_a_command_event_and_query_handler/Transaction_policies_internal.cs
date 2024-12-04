using Compze.Messaging;
using Compze.Messaging.Buses;
using Compze.SystemCE.TransactionsCE;
using Compze.Testing;
using NUnit.Framework;

namespace Compze.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;

public class Transaction_policies_internal : Fixture
{
   [Test] public void Calling_PostRemoteAsync_within_a_transaction_with_AtLeastOnceCommand_throws_TransactionPolicyViolationException() =>
      AssertThrows.Async<MessageInspector.TransactionPolicyViolationException>(() => TransactionScopeCe.Execute(() => ClientEndpoint.ExecuteClientRequest(session => session.PostAsync(MyAtMostOnceCommandWithResult.Create())))).Wait();

   [Test] public void Calling_PostRemoteAsync_within_a_transaction_AtLeastOnceCommand_throws_TransactionPolicyViolationException() =>
      AssertThrows.Exception<MessageInspector.TransactionPolicyViolationException>(() => TransactionScopeCe.Execute(() => ClientEndpoint.ExecuteClientRequest(session => session.Post(MyAtMostOnceCommandWithResult.Create()))));

   [Test] public void Calling_PostRemoteAsync_without_a_transaction_with_ExactlyOnceCommand_throws_TransactionPolicyViolationException() =>
      AssertThrows.Exception<MessageInspector.TransactionPolicyViolationException>(() => RemoteEndpoint.ExecuteServerRequest(session => session.Send(new MyExactlyOnceCommand())));

   [Test] public void Calling_GetRemoteAsync_within_a_transaction_with_Query_throws_TransactionPolicyViolationException() =>
      AssertThrows.Async<MessageInspector.TransactionPolicyViolationException>(() => TransactionScopeCe.Execute(() => ClientEndpoint.ExecuteClientRequest(session => session.GetAsync(new MyQuery())))).Wait();

   [Test] public void Calling_GetRemote_within_a_transaction_with_Query_throws_TransactionPolicyViolationException() =>
      AssertThrows.Exception<MessageInspector.TransactionPolicyViolationException>(() => TransactionScopeCe.Execute(() => ClientEndpoint.ExecuteClientRequest(session => session.Get(new MyQuery()))));

   public Transaction_policies_internal(string _) : base(_) {}
}
