using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Compze.Utilities.Functional;
using Xunit;

namespace Compze.Tests.Infrastructure.XUnit;

/// <summary>
/// Base class for tests that should be run once for each pluggable component combination.
/// Inherit from this class and use [Theory, MemberData(nameof(GetPluggableComponentCombinations), MemberType = typeof(DuplicateByPluggableComponentTest))]
/// on test methods that need to run with all combinations.
/// </summary>
public abstract class DuplicateByPluggableComponentTest : UniversalTestBase
{
   /// <summary>
   /// Provides pluggable component combinations for XUnit Theory tests.
   /// Use as: [Theory, MemberData(nameof(GetPluggableComponentCombinations), MemberType = typeof(DuplicateByPluggableComponentTest))]
   /// </summary>
   public static IEnumerable<object[]> GetPluggableComponentCombinations() =>
      PluggableComponentsReader.GetCombinations()
                               .Select(combination => new object[] { combination });
}

/// <summary>
/// Reads pluggable component combinations from configuration file.
/// </summary>
static class PluggableComponentsReader
{
   const string TestUsingPluggableComponentCombinations = "TestUsingPluggableComponentCombinations";
   
   public static IEnumerable<string> GetCombinations()
   {
      // Try multiple locations to find the file
      var possiblePaths = new[]
      {
         TestUsingPluggableComponentCombinations,
         Path.Combine("..", "..", "..", "..", "..", TestUsingPluggableComponentCombinations),
         Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", TestUsingPluggableComponentCombinations),
      };

      foreach(var path in possiblePaths)
      {
         if(File.Exists(path))
         {
            return File.ReadAllLines(path)
                       .Select(it => it.Trim())
                       .Where(line => !string.IsNullOrEmpty(line))
                       .Where(line => !line.StartsWith('#'))
                       .ToArray();
         }
      }

      // File not found, return error indicator
      return [Enumerable.Repeat("FileMissing", 3)._(it => string.Join(":", it))];
   }
}
