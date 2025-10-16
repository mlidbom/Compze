using System.Linq;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.Infrastructure.XUnit.TestFrameworkExtensions;
using Compze.Utilities.Functional;
using FluentAssertions;

namespace Compze.Tests.Unit.Internals.XUnit;


public class ObjectExtensionsTest : XUnitTestBase
{
   [XF]
   public void RepeatShouldCreateSequenceOfLengthEqualToParameter() => 12.Repeat(10).Count().Should().Be(10);
}