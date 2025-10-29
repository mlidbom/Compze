// ReSharper disable All
#pragma warning disable //Reviewed OK: This is API experimental code that is never ever used.

namespace Compze.Tests.ScratchPad.APIDraft.Policyv2;

// ReSharper disable once UnusedMember.Global
public class ModifyTheDefaults
{
   void IllustratateRegistration()
   {
      var defaultTeventHandlerPolicies = new CompositePolicy(
         Policy.LockExclusively.ThisHandler,   //Ensures that this handler is never invoked in parallel with itself.
         Policy.LockExclusively.CurrentTessage //Ensures that no other handler handle the same queuedTessageInformation in parallel with this handler.
         //Useless when applied to a tommand handler since there can only be one.
      );

      var defaultTommandHandlerPolicies = new CompositePolicy(
         Policy.LockExclusively.TaggregateRelatedToTessage
      );

      var endpoint = new Endpoint(
         //Tommand handlers
         TommandHandler.For<CreateAccountTommand>(
            "17893552-D533-4A59-A177-63EAF3B7B07E",
            tommand => {},
            defaultTommandHandlerPolicies),

         //This tommand handler is completely independent of any other handler since it just sends an email based on the data in the tommand.
         //It can run in parallel with any other handler and itself.
         TommandHandler.For<SendAccountRegistrationWelcomeEmailTommand>("76773E2F-9E44-4150-8C3C-8A4FC93899C3", tommand => {}, Policy.NoRestrictions),

         //Tevent handlers
         TeventHandler.For<AccountCreatedTevent>("2E8642CA-6C60-4B91-A92E-54AD3753E7F2", tevent => {}, defaultTeventHandlerPolicies)
      );
   }
}