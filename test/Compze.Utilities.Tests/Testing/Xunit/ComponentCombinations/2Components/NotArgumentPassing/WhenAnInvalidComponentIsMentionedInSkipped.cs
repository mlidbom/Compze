using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Compze.Contracts;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;
using Compze.Utilities.Testing.Must;
using Xunit.Sdk;

namespace Compze.Utilities.Tests.Testing.Xunit.ComponentCombinations._2Components.NotArgumentPassing;

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
