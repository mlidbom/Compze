using System.Linq;
using Composable.Functional;
using Composable.Testing;
using NUnit.Framework;

namespace Composable.Tests;

[TestFixture]
public class ObjectExtensionsTest : UniversalTestBase
{
   [Test]
   public void RepeatShouldCreateSequenceOfLengthEqualToParameter() => Assert.That(12.Repeat(10).Count(), Is.EqualTo(10));
}