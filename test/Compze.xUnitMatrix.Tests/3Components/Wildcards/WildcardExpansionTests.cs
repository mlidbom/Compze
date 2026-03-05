using Compze.xUnitMatrix;

namespace Compze.xUnitMatrix.Tests._3Components.Wildcards;

public class WildcardExpansionTests(ITestOutputHelper testOutputHelper)
{
   readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

   [WildcardTest]
   public void WildcardsExpandCorrectly(ComponentCombination combination)
   {
      _testOutputHelper.WriteLine($"combination: {combination}");

      // The configuration has:
      // Microsoft:*:*  -> Should expand to all combinations of SqlLayer and DIContainer with Microsoft
      // Newtonsoft:*:Microsoft -> Should expand to all SqlLayers with Newtonsoft and Microsoft DIContainer

      // Total expected:
      // Microsoft:*:* = 2 serializers * 3 SqlLayers * 2 DIContainers = 6 for Microsoft
      // Newtonsoft:*:Microsoft = 3 SqlLayers with Newtonsoft and Microsoft = 3
      // Total = 9 combinations
   }
}
