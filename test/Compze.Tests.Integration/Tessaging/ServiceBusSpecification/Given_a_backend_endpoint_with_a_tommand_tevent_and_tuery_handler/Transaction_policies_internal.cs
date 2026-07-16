using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Abstractions.Tessaging.Validation;
using Compze.Tessaging.Hosting;
using Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Must;
using static Compze.Must.MustActions;

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

public class Transaction_policies_internal : EndpointHostTestBase
{
   [PCT] public async Task Calling_PostRemoteAsync_within_a_transaction_with_AtLeastOnceTommand_throws_TransactionPolicyViolationException() =>
      await InvokingAsync(() => TransactionScopeCe.Execute(() => Navigator.PostAsync(MyAtMostOnceTypermediaTommandWithResult.Create())))
           .Must().ThrowAsync<TessageValidator.TransactionPolicyViolationException>();

   [PCT] public async Task Calling_GetRemoteAsync_within_a_transaction_with_Tuery_throws_TransactionPolicyViolationException() =>
      await InvokingAsync(() => TransactionScopeCe.Execute(() => Navigator.GetAsync(new MyTuery())))
           .Must().ThrowAsync<TessageValidator.TransactionPolicyViolationException>();

   [PCT] public void Calling_PostRemoteAsync_within_a_transaction_AtLeastOnceTommand_throws_TransactionPolicyViolationException() =>
      Invoking(() => TransactionScopeCe.Execute(() => Navigator.Post(MyAtMostOnceTypermediaTommandWithResult.Create())))
        .Must().Throw<TessageValidator.TransactionPolicyViolationException>();

   ///<summary>ToString() rather than Message in the assertion: some backend containers (Autofac) wrap the instantiation's<br/>
   /// exception in their own resolution exception, putting the assert's message on the inner exception.</summary>
   [PCT] public void Resolving_the_unit_of_work_tommand_sender_in_a_scope_without_a_transaction_throws() =>
      Invoking(() => RemoteEndpoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteInIsolatedScope(scope => scope.Resolve<IUnitOfWorkTommandSender>()))
        .Must().Throw<Exception>()
        .Which.ToString().Must().Contain("ambient transaction");

   [PCT] public void Sending_an_ExactlyOnceTommand_through_the_independent_tommand_sender_from_within_an_ambient_transaction_throws() =>
      Invoking(() => TransactionScopeCe.Execute(() => RemoteEndpoint.ServiceLocator.Resolve<IIndependentTommandSender>().Send(new MyExactlyOnceTommand())))
        .Must().Throw<Exception>()
        .Which.Message.Must().Contain("ambient transaction");

   [PCT] public void Calling_GetRemote_within_a_transaction_with_Tuery_throws_TransactionPolicyViolationException() =>
      Invoking(() => TransactionScopeCe.Execute(() => Navigator.Get(new MyTuery())))
        .Must().Throw<TessageValidator.TransactionPolicyViolationException>();
}
