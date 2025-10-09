using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Compze.Tessaging.Hosting.Testing;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

namespace Compze.Tests.Infrastructure.XUnit;

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
}

class PluggableComponentsTheoryDiscoverer : IXunitTestCaseDiscoverer
{
   public ValueTask<IReadOnlyCollection<IXunitTestCase>> Discover(
      ITestFrameworkDiscoveryOptions discoveryOptions,
      IXunitTestMethod testMethod,
      IFactAttribute factAttribute)
   {
      var combinations = PluggableComponentsReader.GetCombinations().ToList();
      
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

      return ValueTask.FromResult<IReadOnlyCollection<IXunitTestCase>>(testCases);
   }
}

class PluggableComponentsTestCase : XunitTestCase
{
   string _combination = string.Empty;

   [Obsolete("Called by deserializer")]
   public PluggableComponentsTestCase()
   {
   }

   public PluggableComponentsTestCase(
      IXunitTestMethod testMethod,
      string combination,
      string testCaseDisplayName,
      string uniqueID,
      bool @explicit,
      int? timeout,
      object?[]? testMethodArguments)
      : base(testMethod, testCaseDisplayName, uniqueID, @explicit, skipReason: null, skipType: null, skipUnless: null, skipWhen: null, timeout: timeout, testMethodArguments: testMethodArguments, traits: null)
   {
      _combination = combination;
   }

   protected override void Serialize(IXunitSerializationInfo info)
   {
      base.Serialize(info);
      info.AddValue(nameof(_combination), _combination);
   }

   protected override void Deserialize(IXunitSerializationInfo info)
   {
      base.Deserialize(info);
      _combination = info.GetValue<string>(nameof(_combination)) ?? string.Empty;
   }
}
