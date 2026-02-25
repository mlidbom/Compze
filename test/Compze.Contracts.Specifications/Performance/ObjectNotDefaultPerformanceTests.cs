using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Performance;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Testing.XUnit.BDD;

namespace Compze.Contracts.Specifications.Performance;

public class ObjectNotDefaultPerformanceTests
{
   [XF] public void Should_run_10_000_tests_in_5_Millisecond()
   {
      const int one = 1;

      TimeAsserter.Execute(
         action: () => ContractAssertion.Argument.NotDefault(one),
         iterations: 10_000,
         maxTotal: 5.Milliseconds().EnvMultiply(instrumented: 6));
   }
}
