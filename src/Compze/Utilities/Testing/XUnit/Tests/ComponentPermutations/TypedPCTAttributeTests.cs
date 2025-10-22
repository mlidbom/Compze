using Compze.Utilities.Testing.XUnit.ComponentPermutations;
using FluentAssertions;
using Xunit;

namespace Compze.Utilities.Testing.XUnit.Tests.ComponentPermutations;

public class TypedPCTAttributeTests
{
   [Fact]
   public void ComponentTypes_Should_Be_Initialized()
   {
      var types = TypedPCTAttribute.ComponentTypes;

      types.Should().NotBeNull();
      types.Should().HaveCount(2);
      types[0].Should().Be(typeof(Serializer));
      types[1].Should().Be(typeof(SqlLayer));
   }
}
