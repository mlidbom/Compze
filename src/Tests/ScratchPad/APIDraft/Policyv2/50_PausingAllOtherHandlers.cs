// ReSharper disable All
#pragma warning disable //Reviewed OK: This is API experimental code that is never ever used.

namespace Compze.Tests.ScratchPad.APIDraft.Policyv2;

public class PausingAllOtherHandlers
{
   void IllustratateRegistration()
   {

      var pauseAllOtherHandlers = new CompositePolicy(
         Policy.LockExclusively.TommandProcessing,
         Policy.LockExclusively.TeventProcessing
      );

      var policiesAsInterfaces = new Endpoint(
         //Tommand handlers
         //various normal tommand and tevent handler registrations

         TommandHandler.For<OptimizeTeventStoreTommand>("F9688A3B-F6AF-4884-9FB5-F6670718F6BE", tommand => { }, pauseAllOtherHandlers),
         TommandHandler.For<OptimizeDocumentDbCommand>("7A2DC4C3-F2DB-43BD-ACB0-BF454BC6C958", tommand => { }, pauseAllOtherHandlers)
      );
   }

}
// ReSharper disable once UnusedMember.Global

class OptimizeDocumentDbCommand {}

class OptimizeTeventStoreTommand {}