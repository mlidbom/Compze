// ReSharper disable All
#pragma warning disable //Reviewed OK: This is API experimental code that is never ever used.

namespace Compze.Tests.ScratchPad.APIDraft.Policyv2;

// ReSharper disable once UnusedMember.Global
public class PuttingItAllTogether
{
   void IllustratateRegistration()
   {
      var defaultEventHandlerPolicies = new CompositePolicy(
         Policy.LockExclusively.ThisHandler,   //Ensures that this handler is never invoked in parallel with itself.
         Policy.LockExclusively.CurrentTessage //Ensures that no other handler handle the same queuedTessageInformation in parallel with this handler.
         //Useless when applied to a tommand handler since there can only be one.
      );

      var defaultTommandHandlerPolicies = new CompositePolicy(
         Policy.LockExclusively.AggregateRelatedToTessage
      );

      var pauseAllOtherHandlers = new CompositePolicy(
         Policy.LockExclusively.TommandProcessing,
         Policy.LockExclusively.EventProcessing
      );

      var endpoint = new Endpoint(
         //Normal tommand handlers
         TommandHandler.For<CreateAccountTommand>(
            "17893552-D533-4A59-A177-63EAF3B7B07E",
            tommand => { },
            defaultTommandHandlerPolicies,
            //Being explicit about which events might be published let's the bus reason about possible cascade effects easily and thus guarantee consistency for queries.
            //It also makes it possible to get an overview of the structure of a complete endpoint in one place.
            Policy.Publishes<IAccountEvent>(),
            //This handler must wait until there are no tessages queued to any handler with policy:
            //Policy.Updates<EmailToAccountLookupModel>. Throws an exception on registration if there are no handlers with matching Updates<> policy.
            Policy.RequiresUpToDate<EmailToAccountLookupModel>.All),

         //This tommand handler is completely independent of any other handler since it just sends an email based on the data in the tommand.
         //It can run in parallel with any other handler and itself.
         TommandHandler.For<SendAccountRegistrationWelcomeEmailTommand>("76773E2F-9E44-4150-8C3C-8A4FC93899C3", tommand => { }, Policy.NoRestrictions),


         //System tommand handlers:
         TommandHandler.For<OptimizeEventStoreTommand>("F9688A3B-F6AF-4884-9FB5-F6670718F6BE", tommand => { }, pauseAllOtherHandlers),
         TommandHandler.For<OptimizeDocumentDbCommand>("7A2DC4C3-F2DB-43BD-ACB0-BF454BC6C958", tommand => { }, pauseAllOtherHandlers),

         //Event handlers
         EventHandler.For<AccountCreatedEvent>(
            "2E8642CA-6C60-4B91-A92E-54AD3753E7F2",
            @event => { },
            defaultEventHandlerPolicies,
            Policy.Updates<AccountReadModel>.WithCurrentTessageAggregateId()),

         EventHandler.For<AccountCreatedEvent>(
            "A5A1DF35-982C-4962-A7DA-C98AC88633C0",
            @event => { },
            defaultEventHandlerPolicies,
            //Being explicit about which tommands might be sent let's the bus reason about possible cascade effects easily and thus guarantee consistency for queries.
            //It also makes it possible to get an overview of the structure of a complete endpoint in one place.
            Policy.Sends<SendAccountRegistrationWelcomeEmailTommand>()
         ),

         EventHandler.For<AccountCreatedEvent>(
            "E59B41A3-BF32-4B7A-B497-F29E3AF42D42",
            @event => { },
            defaultEventHandlerPolicies,
            //(Deprecated. See: Policy.RequiresUpToDate above. )
            //This denormalizer keeps a domain read model up to date. For the domain to work reliably it needs to be executed within the triggering transaction.
            Policy.OnCascadedTessage.InvokeWithinTriggeringTransaction,
            Policy.Updates<EmailToAccountLookupModel>.WithId(new ExtractEmailFromEmailUpdatedEvent())),

         //Delegate to container registered component to handle the event.
         EventHandler.For("6E0EA0E6-67DB-4D25-AFE5-99E67130773D", (AccountCreatedEvent @event, AccountController controller) => controller.Handle(@event)),

         //Generic parameter injection. Actually the same thing as the example above..
         EventHandler.For("85966417-20B9-4373-9A4B-8398ECA86429", (AccountCreatedEvent @event, AccountController dependency1, ISomeDependency dependency2) => { })
      );
   }
}