// ReSharper disable All
#pragma warning disable //Reviewed OK: This is API experimental code that is never ever used.

namespace Compze.Tests.ScratchPad.APIDraft.Policyv2;

// ReSharper disable once UnusedMember.Global
public class BasicHandlerRegistration
{
   void IllustratateRegistration()
   {
      var endpoint = new Endpoint(
         //Tommand handlers
         TommandHandler.For<CreateAccountTommand>("17893552-D533-4A59-A177-63EAF3B7B07E", tommand => {}),

         //Tevent handlers
         TeventHandler.For<AccountCreatedTevent>("2E8642CA-6C60-4B91-A92E-54AD3753E7F2", tevent => {})
      );
   }
}