// ReSharper disable All
#pragma warning disable //Reviewed OK: This is API experimental code that is never ever used.

namespace Compze.Tests.ScratchPad.APIDraft.Policyv2;

// ReSharper disable once UnusedMember.Global
public class EnableCascadeTracking
{
   void IllustratateRegistration()
   {
      var endpoint = new Endpoint(
         //Tommand handlers
         TommandHandler.For<CreateAccountTommand>("17893552-D533-4A59-A177-63EAF3B7B07E", tommand => {},
                                                  //Being explicit about which tevents might be published let's the bus reason about possible cascade effects easily and thus guarantee consistency for tueries.
                                                  //It also makes it possible to get an overview of the structure of a complete endpoint in one place.
                                                  Policy.Publishes<IAccountTevent>()),

         TeventHandler.For<AccountCreatedTevent>("A5A1DF35-982C-4962-A7DA-C98AC88633C0",tevent => {},
                                               //Being explicit about which tommands might be sent let's the bus reason about possible cascade effects easily and thus guarantee consistency for tueries.
                                               //It also makes it possible to get an overview of the structure of a complete endpoint in one place.
                                               Policy.Sends<SendAccountRegistrationWelcomeEmailTommand>()
         )
      );
   }
}