using System.Linq;
using Compze.Tests.Infrastructure;
using Compze.Utilities.Functional;
using NUnit.Framework;
using Compze.Tests.Infrastructure.NUnit;

namespace Compze.Tests.Unit.Internals;

[TestFixture]
public class ObjectExtensionsTest : UniversalTestBase
{
   [Test]
   public void RepeatShouldCreateSequenceOfLengthEqualToParameter() => Assert.That(12.Repeat(10).Count(), Is.EqualTo(10));
}