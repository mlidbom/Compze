using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Compze.Tests.Infrastructure.NUnit;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes : This class is instantiated by NUnit via reflection.

[TestFixture, TestFixtureSource(typeof(PluggableComponentsTestFixtureSource))]
public class DuplicateByPluggableComponentTest : UniversalTestBase
{
#pragma warning disable IDE0060, CA1801 // Remove unused parameter : There parameter value is used by NUnit in naming the test and then by composable via reflection of the NUnit API.
   public DuplicateByPluggableComponentTest(string pluggableComponentsCombination) {}
#pragma warning restore IDE0060, CA1801 // Remove unused parameter
}

class PluggableComponentsTestFixtureSource : IEnumerable<string>
{
   static readonly IReadOnlyList<PluggableComponents> Dimensions = PluggableComponentsReader.GetCombinations();
   static readonly IReadOnlyList<string> DimensionsStrings = PluggableComponentsReader.GetCombinations().Select(it => it.ToString()).ToList();
   public IEnumerator<string> GetEnumerator() => DimensionsStrings.GetEnumerator();
   IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

#pragma warning restore CA1812
