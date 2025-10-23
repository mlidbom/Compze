using Compze.Utilities.Testing.XUnit.ComponentsCombinations;
using Compze.Utilities.Testing.XUnit.Tests.ComponentCombinations._2Components;
using FluentAssertions;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentCombinations._3Components.Wildcards;

public class WildcardFeatureDocumentation
{
   [WildcardTest] public void WildcardConfigurationGeneratesCorrectPermutations(ComponentsCombination combination)
   {
      combination.Components.Should().HaveCount(3);
      combination.Components[0].Should().BeOfType<Serializer>();
      combination.Components[1].Should().BeOfType<SqlLayer>();
      combination.Components[2].Should().BeOfType<DIContainer>();
   }
}
