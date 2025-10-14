using System.Linq;
using Compze.Tests.Infrastructure;
using Compze.Utilities.Functional;
using Xunit;
using Compze.Tests.Infrastructure.XUnit;
using FluentAssertions;

namespace Compze.Tests.Unit.Internals;


public class ObjectExtensionsTest : XUnitTestBase
{
   [Fact]
   public void RepeatShouldCreateSequenceOfLengthEqualToParameter() => 12.Repeat(10).Count().Should().Be(10);
}