using Xunit.Sdk;

namespace Compze.xUnitMatrix._private;

/// <summary>
/// The test runner's discovery options as <see cref="MatrixTheoryDiscoverer"/> passes them on to xUnit's theory discovery:<br/>
/// identical to the runner's in every respect, except that theory pre-enumeration is always on.<br/>
/// A read-only view: xUnit's theory discovery reads it, nothing writes it.
/// </summary>
/// <remarks>
/// Pre-enumeration is xUnit expanding a theory into one test case per data row at discovery time, rather than one test case
/// that enumerates its rows when it runs. A matrix test needs the expansion: each combination is a data row, and each
/// <see cref="MatrixCombinationTestCase"/> carries its combination as its test method argument.<br/>
/// Whether pre-enumeration is on is the runner's choice: <c>dotnet test</c> turns it on, a test executable run directly
/// leaves it off. Overriding it for matrix theories alone, instead of changing the runner's shared options, leaves every
/// other theory in the assembly exactly as the runner configured it.
/// </remarks>
class ReadOnlyDiscoveryOptionsWithTheoryPreEnumeration(ITestFrameworkDiscoveryOptions runnerDiscoveryOptions) : ITestFrameworkDiscoveryOptions
{
   readonly ITestFrameworkDiscoveryOptions _runnerDiscoveryOptions = runnerDiscoveryOptions;

   public TValue? GetValue<TValue>(string name) =>
      name == TestOptionsNames.Discovery.PreEnumerateTheories
         ? (TValue)(object)true
         : _runnerDiscoveryOptions.GetValue<TValue>(name);

   public void SetValue<TValue>(string name, TValue value) => throw new NotSupportedException(OnlyReadByTheoryDiscovery);

   public string ToJson() => throw new NotSupportedException(OnlyReadByTheoryDiscovery);

   const string OnlyReadByTheoryDiscovery = $"{nameof(ReadOnlyDiscoveryOptionsWithTheoryPreEnumeration)} is a read-only view that only xUnit's theory discovery reads.";
}
