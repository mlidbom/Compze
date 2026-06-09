using Compze.Contracts;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Xunit.Sdk;
using Xunit.v3;

namespace Compze.xUnitMatrix.Tests._2Components;

public class WhenAnInvalidComponentIsMentionedInSkipped
{
   [TwoComponentMatrix]
   [Skip<DIContainer>(DIContainer.Microsoft, "test reason")]
   public async Task TheTestIsSkippedWithAnErrorTessage()
   {
      await using var disposalTracker = new DisposalTracker();

      var attribute = new TwoComponentMatrixAttribute();
      var testData = await ((IDataAttribute)attribute).GetData(typeof(WhenAnInvalidComponentIsMentionedInSkipped)
                                            .GetMethod(nameof(TheTestIsSkippedWithAnErrorTessage))._assert().NotNull(), disposalTracker).caf();

      testData.Must().HaveCount(1);
      testData.Single().Skip!.Must().Contain("DIContainer");
   }
}
