using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Threading.TasksCE;
using FluentAssertions;
using Xunit.Sdk;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentCombinations._2Components.NotArgumentPassing;

public class WhenAnInvalidComponentIsMentionedInSkipped
{
   [NotArgumentPassingTwoComponentsPCT]
   public async Task TheTestIsSkippedWithAnErrorTessage()
   {
      var testData = await new NotArgumentPassingTwoComponentsPCTAttribute()
                           {
                              Skipped = ["nonsense"],
                              SkipReasons = ["because something"]
                           }.GetData(MethodBase.GetCurrentMethod().NotNull().CastTo<MethodInfo>(), new DisposalTracker()).caf();

      testData.Should().HaveCount(1);
      testData.Single().Skip.Should().Contain("nonsense");
   }
}
