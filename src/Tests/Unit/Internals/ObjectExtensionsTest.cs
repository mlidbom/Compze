using System.Linq;
using Compze.Tests.Infrastructure;
using Compze.Utilities.Functional;
using NUnit.Framework;
using Compze.Tests.Infrastructure.NUnit;
using FluentAssertions;

namespace Compze.Tests.Unit.Internals;

[TestFixture]
public class ObjectExtensionsTest : NUnitTestBase
{
   [Test]
   public void RepeatShouldCreateSequenceOfLengthEqualToParameter() => 12.Repeat(10).Count().Should().Be(10);
}