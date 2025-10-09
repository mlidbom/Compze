using System.Linq;
using Compze.TestInfrastructure;
using Compze.Utilities.Functional;
using NUnit.Framework;

namespace Compze.Tests.Unit.Internals;

[TestFixture]
public class ObjectExtensionsTest : UniversalTestBase
{
   [Test]
   public void RepeatShouldCreateSequenceOfLengthEqualToParameter() => Assert.That(12.Repeat(10).Count(), Is.EqualTo(10));
}