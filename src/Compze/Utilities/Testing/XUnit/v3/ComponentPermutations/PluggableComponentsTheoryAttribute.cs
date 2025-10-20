using System.Reflection;
using System.Runtime.CompilerServices;
using Compze.Utilities.Testing.XUnit.ComponentPermutations;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.Utilities.Testing.XUnit.v3.ComponentPermutations;
#pragma warning disable CA1813 //avoid unsealed attributes

// ReSharper disable GrammarMistakeInComment
/// <summary>
/// Pluggable Components Theory Attribute
/// Use this attribute instead of [XFact] for tests that should run with all pluggable component combinations.
/// Automatically discovers combinations and injects a PluggableComponentTestContext into TestEnv.
/// Use TestEnv to access the component and the information.
/// </summary>
[XunitTestCaseDiscoverer(typeof(TheoryDiscoverer))] // Use standard TheoryDiscoverer!
public class PluggableComponentsTheoryAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1) :
   TheoryAttribute(sourceFilePath, sourceLineNumber),
   IDataAttribute
{
   public string[] Exclude { get; init; } = [];

   // Skip tests without data instead of failing (for inherited tests in derived classes)
   public new bool SkipTestWithoutData { get; set; } = true;

   // IDataAttribute implementation - we supply our own data!
   bool? IDataAttribute.Explicit => Explicit;
   string? IDataAttribute.Label => null;
   string? IDataAttribute.Skip => Skip;
   Type? IDataAttribute.SkipType => SkipType;
   string? IDataAttribute.SkipUnless => SkipUnless;
   string? IDataAttribute.SkipWhen => SkipWhen;
   string? IDataAttribute.TestDisplayName => DisplayName;
   int? IDataAttribute.Timeout => Timeout > 0 ? Timeout : null;
   string[]? IDataAttribute.Traits => null;

   public ValueTask<IReadOnlyCollection<ITheoryDataRow>> GetData(MethodInfo testMethod, DisposalTracker disposalTracker)
   {
      if(testMethod.DeclaringType != testMethod.ReflectedType) //Only run for the class that declares the test method.
      {
         return new ValueTask<IReadOnlyCollection<ITheoryDataRow>>(
            [
               new TheoryDataRow("skipped") { Skip = "Only runs in declaring class" }
            ]);
      }

      var permutations = PluggableComponentsReader
                        .Permutations
                        .Exclude(Exclude)
                        .Select(permutation => new TheoryDataRow(permutation.ToString()) as ITheoryDataRow)
                        .ToArray();

      return new ValueTask<IReadOnlyCollection<ITheoryDataRow>>(permutations);
   }

   public bool SupportsDiscoveryEnumeration() => true; // Yes, we can enumerate at discovery time
}

/// <summary>
/// Alias for PluggableComponentsTheoryAttribute
/// Pluggable Components Theory Attribute
/// Use this attribute instead of [XFact] for tests that should run with all pluggable component combinations.
/// Automatically discovers combinations and injects a PluggableComponentTestContext into TestEnv.
/// Use TestEnv to access the component and the information.
/// </summary>
public sealed class PCTAttribute([CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = -1)
   : PluggableComponentsTheoryAttribute(sourceFilePath, sourceLineNumber) {}
#pragma warning restore CA1813 //avoid unsealed attributes
