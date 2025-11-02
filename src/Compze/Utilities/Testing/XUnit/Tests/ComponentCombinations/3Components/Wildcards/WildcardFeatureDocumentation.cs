using Compze.Utilities.Testing.XUnit.ComponentCombinations;
using Compze.Utilities.Testing.XUnit.Tests.ComponentCombinations._2Components;
using Compze.Utilities.Testing.Fluent;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentCombinations._3Components.Wildcards;

public class WildcardFeatureDocumentation
{
   [WildcardTest] public void WildcardConfigurationGeneratesCorrectPermutations(ComponentCombination combination)
   {
      combination.Components.Must().HaveCount(3);
      combination.Components[0].Must().BeOfType<Serializer>();
      combination.Components[1].Must().BeOfType<SqlLayer>();
      combination.Components[2].Must().BeOfType<DIContainer>();
   }
}
