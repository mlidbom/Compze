using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Compze.Tests.Infrastructure.NUnit;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes : This class is instantiated by NUnit via reflection.

[TestFixture, TestFixtureSource(typeof(PluggableComponentsTestFixtureSource))]
public class DuplicateByPluggableComponentTest : UniversalTestBase
{
#pragma warning disable IDE0060, CA1801 // Remove unused parameter : The parameter value is used by NUnit in naming the test and then by composable via inspecting the test name.
   public DuplicateByPluggableComponentTest(string pluggableComponentsCombination){}
#pragma warning restore IDE0060, CA1801 // Remove unused parameter
}

class PluggableComponentsTestFixtureSource : IEnumerable<string>
{
   static readonly IReadOnlyList<string> DimensionsStrings = PluggableComponentsReader.GetCombinations().Select(it => it.ToString()).ToList();
   public IEnumerator<string> GetEnumerator() => DimensionsStrings.GetEnumerator();
   IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

#pragma warning restore CA1812
