using FluentAssertions;
using Compze.Utilities.Testing.XUnit.ComponentPermutations;
using Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations._2Components;
using Xunit;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations._3Components.Wildcards;

public class WildcardFeatureDocumentation
{
   [WildcardTest] public void WildcardConfigurationGeneratesCorrectPermutations(ComponentsPermutation permutation)
   {
      permutation.Components.Should().HaveCount(3);
      permutation.Components[0].Should().BeOfType<Serializer>();
      permutation.Components[1].Should().BeOfType<SqlLayer>();
      permutation.Components[2].Should().BeOfType<DIContainer>();
   }
}
