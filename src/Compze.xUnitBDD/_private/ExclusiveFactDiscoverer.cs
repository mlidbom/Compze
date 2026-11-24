using Xunit.Internal;
using Xunit.Sdk;
using Xunit.v3;
using Compze.xUnit._internal;

namespace Compze.xUnitBDD._private;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes : This class is instantiated by xUnit via reflection.
class ExclusiveFactDiscoverer : IXunitTestCaseDiscoverer
{
   public ValueTask<IReadOnlyCollection<IXunitTestCase>> Discover(
      ITestFrameworkDiscoveryOptions discoveryOptions,
      IXunitTestMethod testMethod,
      IFactAttribute factAttribute)
   {
      var declaringType = testMethod.Method.DeclaringType!;

      var isInherited = declaringType != testMethod.TestClass.Class;

      //xUnit never runs an abstract class, so the fact would silently run nowhere. It fails loudly instead (Magnus, 2026-11-24).
      if(isInherited && declaringType.IsAbstract)
         return TestCasesAsync(ErrorThatTheFactWouldNeverRun());

      if(isInherited) //We only run these tests for the classes that declares them.
         return TestCasesAsync();

      #pragma warning disable CA2000// We are passing this disposable into a constructor of an object we don't own
      return TestCasesAsync(new ExclusiveFactTestCase(
                          details: Details(),
                          traits: testMethod.Traits.ToReadWrite(StringComparer.OrdinalIgnoreCase)));
      #pragma warning restore CA2000

      static ValueTask<IReadOnlyCollection<IXunitTestCase>> TestCasesAsync(params IXunitTestCase[] testCases) => new(testCases);

      TestCaseDetails Details() => new(TestIntrospectionHelper.GetTestCaseDetails(discoveryOptions, testMethod, factAttribute));

      IXunitTestCase ErrorThatTheFactWouldNeverRun()
      {
         var details = Details();
         return new ExecutionErrorTestCase(
            testMethod,
            details.TestCaseDisplayName,
            details.UniqueID,
            details.SourceFilePath,
            details.SourceLineNumber,
            $"[XF] runs a fact only in the class that declares it, and {declaringType}, which declares {testMethod.Method.Name}, is abstract. " +
            $"xUnit never runs an abstract class, so the fact would run nowhere. To run it in every class derived from {declaringType}, make it a [Fact].");
      }
   }
}
#pragma warning restore CA1812 // Avoid uninstantiated internal classes : This class is instantiated by xUnit via reflection.
