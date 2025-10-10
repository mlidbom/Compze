using System;
using System.Collections.Generic;
using System.Linq;
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