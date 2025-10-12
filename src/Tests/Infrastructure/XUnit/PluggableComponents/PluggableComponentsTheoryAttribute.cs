using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Compze.Tessaging.Hosting.Testing;
using Compze.Utilities.SystemCE;
using Compze.Wiring;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

namespace Compze.Tests.Infrastructure.XUnit.PluggableComponents;

/// <summary>
/// Use this attribute instead of [Fact] for tests that should run with all pluggable component combinations.
/// Automatically discovers combinations and injects a PluggableComponentTestContext instance.
/// </summary>
[XunitTestCaseDiscoverer(typeof(PluggableComponentsTheoryDiscoverer))]
public sealed class PluggableComponentsTheoryAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1)
   : FactAttribute(sourceFilePath, sourceLineNumber)
{
   static PluggableComponentsTheoryAttribute()
   {
      TestFixtureHelper.SetupSerilog(null);
   }

   /// <summary>
   /// SQL layers to exclude from test execution. Use when a test is not applicable to certain database types.
   /// </summary>
   public Wiring.SqlLayer[]? ExcludeSqlLayers { get; init; }
}
#pragma warning disable CA1812 // Avoid uninstantiated internal classes : This class is instantiated by xUnit via reflection.
class PluggableComponentsTheoryDiscoverer : IXunitTestCaseDiscoverer
{
#pragma warning restore CA1812 // Avoid uninstantiated internal classes : This class is instantiated by xUnit via reflection.
   public async ValueTask<IReadOnlyCollection<IXunitTestCase>> Discover(
      ITestFrameworkDiscoveryOptions discoveryOptions,
      IXunitTestMethod testMethod,
      IFactAttribute factAttribute)
   {
      var combinations = PluggableComponentsReader.GetCombinations();

      // Filter out excluded SQL layers if specified
      if(factAttribute is PluggableComponentsTheoryAttribute { ExcludeSqlLayers.Length: > 0 } theoryAttribute)
      {
         var excludedLayers = theoryAttribute.ExcludeSqlLayers;
         combinations = combinations
                       .Where(combo =>
                        {
                           var context = new PluggableComponentTestContext(combo);
                           return !excludedLayers.Contains(TestEnv.SqlLayer);
                        })
                       .ToList();
      }

      var testCases = combinations
                     .Select(combination =>
                      {
                         // Create and pass a PluggableComponentTestContext instance
                         var arguments = new object[] { new PluggableComponentTestContext(combination) };

                         return new PluggableComponentsTestCase(
                            testMethod: testMethod,
                            combination: combination,
                            testCaseDisplayName: $"{testMethod.Method.Name}({combination})",
                            uniqueID: $"{testMethod.UniqueID}.{combination}",
                            @explicit: factAttribute.Explicit,
                            timeout: factAttribute.Timeout,
                            testMethodArguments: arguments);
                      })
                     .ToArray();

      return await ValueTask.FromResult<IReadOnlyCollection<IXunitTestCase>>(testCases);
   }
}

class PluggableComponentsTestCase : XunitTestCase
{
   Infrastructure.PluggableComponents? _combination = null;

   [Obsolete("Called by deserializer")]
   public PluggableComponentsTestCase() {}

   public PluggableComponentsTestCase(
      IXunitTestMethod testMethod,
      Infrastructure.PluggableComponents combination,
      string testCaseDisplayName,
      string uniqueID,
      bool @explicit,
      int? timeout,
      object?[]? testMethodArguments)
      : base(testMethod,
             testCaseDisplayName,
             uniqueID,
             @explicit,
             skipReason: null,
             skipType: null,
             skipUnless: null,
             skipWhen: null,
             timeout: timeout,
             testMethodArguments: testMethodArguments,
             traits: null) =>
      _combination = combination;

   protected override void Serialize(IXunitSerializationInfo info)
   {
      base.Serialize(info);
      info.AddValue(nameof(_combination), _combination.ToString());
   }

   protected override void Deserialize(IXunitSerializationInfo info)
   {
      base.Deserialize(info);
      _combination = Infrastructure.PluggableComponents.FromString(info.GetValue<string>(nameof(_combination)).NotNull());
   }
}
