using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Compze.Testing;

[TestFixture, TestFixtureSource(typeof(PluggableComponentsTestFixtureSource))]
public class DuplicateByPluggableComponentTest : UniversalTestBase
{
#pragma warning disable IDE0060, CA1801 // Remove unused parameter : There parameter value is used by NUnit in naming the test and then by composable via reflection of the NUnit API.
   public DuplicateByPluggableComponentTest(string pluggableComponentsCombination) {}
#pragma warning restore IDE0060, CA1801 // Remove unused parameter
}

class PluggableComponentsTestFixtureSource : IEnumerable<string>
{
   static readonly List<string> Dimensions = CreateDimensions().ToList();
   public IEnumerator<string> GetEnumerator() => Dimensions.GetEnumerator();
   IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

   const string TestUsingPluggableComponentCombinations = "TestUsingPluggableComponentCombinations";
   static string[] CreateDimensions()
   {
      try
      {
         return File.ReadAllLines(TestUsingPluggableComponentCombinations)
                    .Select(it => it.Trim())
                    .Where(line => !string.IsNullOrEmpty(line))
                    .Where(line => !line.StartsWith('#'))
                    .ToArray();
      }
      catch(Exception e)
      {
         return [e.ToString()];
      }
   }
}