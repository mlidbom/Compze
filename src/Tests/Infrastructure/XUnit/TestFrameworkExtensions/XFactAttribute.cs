using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

namespace Compze.Tests.Infrastructure.XUnit.TestFrameworkExtensions;

/// <summary>
/// This attribute will run the test exclusively for the class that declares the test. It will not be executed when inheriting classes run their tests.
/// This enables us to use BDD style nested classes with inheritance to achieve specification like testing, without an explosion of duplicated test runs.
/// </summary>
[XunitTestCaseDiscoverer(typeof(XFactAttributeTestCaseDiscoverer))]
public sealed class XFactAttribute(
      [CallerFilePath] string? sourceFilePath = null,
      [CallerLineNumber] int sourceLineNumber = -1)
   // ReSharper disable once ExplicitCallerInfoArgument
   : FactAttribute(sourceFilePath,
                   // ReSharper disable once ExplicitCallerInfoArgument
                   sourceLineNumber) {}

public class XFactAttributeTestCaseDiscoverer : IXunitTestCaseDiscoverer
{
   public ValueTask<IReadOnlyCollection<IXunitTestCase>> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, IXunitTestMethod testMethod, IFactAttribute factAttribute)
   {
      var declaringType = testMethod.Method.DeclaringType;
      var currentType = testMethod.TestClass.Class;

      // Ensure we have valid types before comparing
      // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
      if(declaringType == null || currentType == null)
         return ValueTask.FromResult<IReadOnlyCollection<IXunitTestCase>>([]);

      if(declaringType != currentType)
         return ValueTask.FromResult<IReadOnlyCollection<IXunitTestCase>>([]);

      // Build deterministic ID from full type name + method name instead of relying on testMethod.UniqueID
      // This ensures NCrunch gets the same ID during discovery and execution phases
      var fullName = testMethod.TestClass.Class.FullName ?? testMethod.TestClass.Class.Name;
      var methodName = testMethod.Method.Name ?? "UnknownMethod";
      var stableUniqueId = $"{fullName}.{methodName}";

      // XUnit v3 requires that SkipUnless and SkipWhen are mutually exclusive
      // Only pass non-null/non-empty values, and ensure both aren't set
      var skipUnless = !string.IsNullOrEmpty(factAttribute.SkipUnless) ? factAttribute.SkipUnless : null;
      var skipWhen = !string.IsNullOrEmpty(factAttribute.SkipWhen) ? factAttribute.SkipWhen : null;

      // If both are somehow set, prefer SkipUnless (defensive)
      if(skipUnless != null && skipWhen != null)
         skipWhen = null;

      return ValueTask.FromResult<IReadOnlyCollection<IXunitTestCase>>([
                                                                          new XunitTestCase(
                                                                             testMethod: testMethod,
                                                                             testCaseDisplayName: methodName,
                                                                             uniqueID: stableUniqueId,
                                                                             @explicit: factAttribute.Explicit,
                                                                             skipReason: factAttribute.Skip,
                                                                             skipType: factAttribute.SkipType,
                                                                             skipUnless: skipUnless,
                                                                             skipWhen: skipWhen,
                                                                             timeout: factAttribute.Timeout,
                                                                             testMethodArguments: [],
                                                                             traits: new Dictionary<string, HashSet<string>>())
                                                                       ]);
   }
}
