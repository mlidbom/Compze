using Compze.Internals.Testing;
using Compze.Internals.Testing.Performance;
using Compze.Internals.SystemCE;
using Compze.xUnitBDD;

namespace Compze.Contracts.Specifications.Performance;

public class ObjectNotDefaultPerformanceTests
{
   [XF] public void Should_run_10_000_tests_in_5_Millisecond()
   {
      const int one = 1;

      TimeAsserter.Execute(
         action: () => Contract.Argument.NotDefault(one),
         iterations: 10_000,
         maxTotal: 5.Milliseconds().EnvMultiply(instrumented: 6));
   }
}
