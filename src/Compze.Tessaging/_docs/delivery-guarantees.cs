// ReSharper disable All

using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageTypes;
using Compze.TypeIdentifiers.DependencyInjection;
using Compze.Tessaging.Documentation.DeclaringEndpoints;

#pragma warning disable // Documentation example code: deliberately illustrative, not production code.

namespace Compze.Tessaging.Documentation.DeliveryGuarantees;

#region declaring_a_retry_policy
class BillingEndpoint : ExactlyOnceEndpointDeclaration<BillingEndpoint>, IEndpointIdentity
{
   public static string Name => "Billing";
   public static EndpointId Id { get; } = new(Guid.Parse("4D7A2E91-B6C3-4F58-8A10-E39C5B72D6F4"));

   //A tessage whose handling keeps failing is tried for 30 minutes of failing, instead of the default 5, before it is marked failed.
   protected override RetryPolicy? RetryPolicy => RetryPolicy.GiveUpAfter(TimeSpan.FromMinutes(30));

   protected override void RegisterComponents(IComponentRegistrar registrar) => registrar
      .RequireMappedTypesFromAssemblyContaining<RegisterAccount>();
}
#endregion

#region deferring_a_tommand_handler
public class SendWelcomeEmail(Guid accountId, string email) : Remotable.ExactlyOnce.Tommand
{
   public Guid AccountId { get; private set; } = accountId;
   public string Email { get; private set; } = email;
}

class OnboardingEndpoint : ExactlyOnceEndpointDeclaration<OnboardingEndpoint>, IEndpointIdentity
{
   public static string Name => "Onboarding";
   public static EndpointId Id { get; } = new(Guid.Parse("8C2E5F17-4A93-4B6D-9E01-D7B36A58C4F2"));

   protected override void RegisterComponents(IComponentRegistrar registrar) => registrar
      .RequireMappedTypesFromAssemblyContaining<RegisterAccount>()
      .Register(Singleton.For<IEmailSender>().CreatedBy(() => new ConsoleEmailSender()));

   //Runs within the unit of work of whoever sends RegisterAccount: the registration and its next step commit together.
   protected override void RegisterExactlyOnceTommandHandlers(IExactlyOnceTommandHandlerRegistrar handle) => handle
      .ForTommand((RegisterAccount tommand, IUnitOfWorkTommandSender tommandSender) =>
                     tommandSender.SendAsync(new SendWelcomeEmail(tommand.AccountId, tommand.Email)));

   //Never runs within its sender's unit of work: once the registration has committed, the endpoint's inbox runs it in a
   //unit of work of its own, and tries it again while it fails. One account's welcome email needn't wait for another's,
   //so it gives up its place in the handling order: one that keeps failing holds up no other.
   protected override void RegisterDeferredTommandHandlers(IDeferredTommandHandlerRegistrar handle) => handle
      .ForTommand((SendWelcomeEmail tommand, IUnitOfWorkResolver unitOfWork) =>
                     unitOfWork.Resolve<IEmailSender>().SendWelcomeEmailAsync(tommand.Email),
                  HandlingOrder.None);
}
#endregion
