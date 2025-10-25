using System.Threading.Tasks;
using Compze.Abstractions.Tessaging.Teventive.Infrastructure.Validation;
using Compze.Tessaging.Hosting;
using Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_event_and_tuery_handler;
using Compze.Utilities.SystemCE.TransactionsCE;
using Compze.Tests.Infrastructure.XUnit;
using FluentAssertions;
using static FluentAssertions.FluentActions;

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_event_and_tuery_handler;

public class Transaction_policies_internal : EndpointHostTestBase
{
   [PCT] public async Task Calling_PostRemoteAsync_within_a_transaction_with_AtLeastOnceTommand_throws_TransactionPolicyViolationException() =>
      await Invoking(() => TransactionScopeCe.Execute(() => ClientEndpoint.ExecuteClientRequest(session => session.PostAsync(MyAtMostOnceTommandWithResult.Create()))))
           .Should().ThrowAsync<TessageInspector.TransactionPolicyViolationException>();

   [PCT] public async Task Calling_GetRemoteAsync_within_a_transaction_with_Tuery_throws_TransactionPolicyViolationException() =>
      await Invoking(() => TransactionScopeCe.Execute(() => ClientEndpoint.ExecuteClientRequest(session => session.GetAsync(new MyTuery()))))
           .Should().ThrowAsync<TessageInspector.TransactionPolicyViolationException>();

   [PCT] public void Calling_PostRemoteAsync_within_a_transaction_AtLeastOnceTommand_throws_TransactionPolicyViolationException() =>
      Invoking(() => TransactionScopeCe.Execute(() => ClientEndpoint.ExecuteClientRequest(session => session.Post(MyAtMostOnceTommandWithResult.Create()))))
        .Should().Throw<TessageInspector.TransactionPolicyViolationException>();

   [PCT] public void Calling_PostRemoteAsync_without_a_transaction_with_ExactlyOnceTommand_throws_TransactionPolicyViolationException() =>
      Invoking(() => RemoteEndpoint.ExecuteServerRequest(session => session.Send(new MyExactlyOnceTommand())))
        .Should().Throw<TessageInspector.TransactionPolicyViolationException>();

   [PCT] public void Calling_GetRemote_within_a_transaction_with_Tuery_throws_TransactionPolicyViolationException() =>
      Invoking(() => TransactionScopeCe.Execute(() => ClientEndpoint.ExecuteClientRequest(session => session.Get(new MyTuery()))))
        .Should().Throw<TessageInspector.TransactionPolicyViolationException>();
}
