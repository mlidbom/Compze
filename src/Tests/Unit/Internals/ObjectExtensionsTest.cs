using System.Linq;
using Compze.Tests.Infrastructure;
using Compze.Utilities.Functional;
using Compze.Utilities.Testing.XUnit.BDD;
using FluentAssertions;

namespace Compze.Tests.Unit.Internals;


public class ObjectExtensionsTest : UniversalTestBase
{
   [XF]
   public void RepeatShouldCreateSequenceOfLengthEqualToParameter() => 12.Repeat(10).Count().Should().Be(10);
}