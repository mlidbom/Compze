using System.Linq;
using Compze.Functional;
using Compze.Testing;
using NUnit.Framework;

namespace Compze.Tests.Unit.Internals;

[TestFixture]
public class ObjectExtensionsTest : UniversalTestBase
{
   [Test]
   public void RepeatShouldCreateSequenceOfLengthEqualToParameter() => Assert.That(12.Repeat(10).Count(), Is.EqualTo(10));
}