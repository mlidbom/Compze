using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Compze.Tests.Infrastructure.NUnit;

[TestFixture, TestFixtureSource(typeof(PluggableComponentsTestFixtureSource))]
public class DuplicateByPluggableComponentTest : UniversalTestBase
{
#pragma warning disable IDE0060, CA1801 // Remove unused parameter : There parameter value is used by NUnit in naming the test and then by composable via reflection of the NUnit API.
   public DuplicateByPluggableComponentTest(string pluggableComponentsCombination) {}
#pragma warning restore IDE0060, CA1801 // Remove unused parameter
}

class PluggableComponentsTestFixtureSource : IEnumerable<string>
{
   static readonly List<string> Dimensions = PluggableComponentsReader.GetCombinations().ToList();
   public IEnumerator<string> GetEnumerator() => Dimensions.GetEnumerator();
   IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}