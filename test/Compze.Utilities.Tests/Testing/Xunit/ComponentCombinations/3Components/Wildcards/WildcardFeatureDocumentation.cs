using Compze.Utilities.Testing.XUnit.ComponentCombinations;
using Compze.Utilities.Tests.Testing.Xunit.ComponentCombinations._2Components;

namespace Compze.Utilities.Tests.Testing.Xunit.ComponentCombinations._3Components.Wildcards;

public class WildcardFeatureDocumentation
{
   [WildcardTest] public void WildcardConfigurationGeneratesCorrectPermutations(ComponentCombination combination)
   {
      combination.Components.Must().HaveCount(3);
      combination.Components[0].Must().BeExactType<Serializer>();
      combination.Components[1].Must().BeExactType<SqlLayer>();
      combination.Components[2].Must().BeExactType<DIContainer>();
   }
}
