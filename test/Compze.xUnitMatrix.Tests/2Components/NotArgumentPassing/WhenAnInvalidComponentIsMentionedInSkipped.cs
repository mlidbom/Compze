using System.Reflection;
using Compze.Contracts;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Xunit.Sdk;

namespace Compze.xUnitMatrix.Tests._2Components.NotArgumentPassing;

public class WhenAnInvalidComponentIsMentionedInSkipped
{
   [NotArgumentPassingTwoComponentsPCT]
   public async Task TheTestIsSkippedWithAnErrorTessage()
   {
      await using var disposalTracker = new DisposalTracker();
      var testData = await new NotArgumentPassingTwoComponentsPCTAttribute()
                           {
                              Skipped = ["nonsense"],
                              SkipReasons = ["because something"]
                           }.GetData(MethodBase.GetCurrentMethod()._assert().NotNull().CastTo<MethodInfo>(), disposalTracker).caf();

      testData.Must().HaveCount(1);
      testData.Single().Skip!.Must().Contain("nonsense");
   }
}
