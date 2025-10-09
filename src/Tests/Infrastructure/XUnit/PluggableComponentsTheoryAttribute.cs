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
/// Simplified attribute that generates test cases for all pluggable component combinations.
/// The test method must accept a string parameter for the combination.
/// TestEnv.SetTestContext() will be called automatically with this parameter.
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
      
      // Check if method has parameters
      var parameters = testMethod.Method.GetParameters();
      var hasStringParameter = parameters.Any() && parameters[0].ParameterType.Name == "String";
      
      var testCases = combinations
         .Select(combination =>
         {
            // If method has a string parameter, pass the combination as an argument
            var arguments = hasStringParameter ? new object[] { combination } : null;
            
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
