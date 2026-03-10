using Compze.xUnitMatrix.Tests._2Components;

namespace Compze.xUnitMatrix.Tests._3Components.Wildcards;

public class WildcardFeatureDocumentation
{
   [WildcardTest] public void WildcardConfigurationGeneratesCorrectPermutations(MatrixCombination combination)
   {
      combination.Components.Must().HaveCount(3);
      combination.Components[0].Must().BeExactType<Serializer>();
      combination.Components[1].Must().BeExactType<SqlLayer>();
      combination.Components[2].Must().BeExactType<DIContainer>();
   }
}
