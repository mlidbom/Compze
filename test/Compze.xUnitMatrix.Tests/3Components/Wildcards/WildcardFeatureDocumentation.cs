using Compze.xUnitMatrix.Tests._2Components;

namespace Compze.xUnitMatrix.Tests._3Components.Wildcards;

public class WildcardFeatureDocumentation
{
   [WildcardTest] public void WildcardConfigurationGeneratesCorrectCombinations()
   {
      var combination = MatrixCombination.Current;
      combination.DimensionValues.Must().HaveCount(3);
      combination.DimensionValues[0].Must().BeExactType<Serializer>();
      combination.DimensionValues[1].Must().BeExactType<SqlLayer>();
      combination.DimensionValues[2].Must().BeExactType<DIContainer>();
   }
}
