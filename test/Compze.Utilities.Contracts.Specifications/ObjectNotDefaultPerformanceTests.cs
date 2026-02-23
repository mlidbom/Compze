using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Performance;
using Compze.Tests.Infrastructure;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Testing.XUnit.BDD;
using Assert = Compze.Utilities.Contracts.Assert;

namespace Compze.Utilities.Contracts.Specifications;

public class ObjectNotDefaultPerformanceTests : UniversalTestBase
{
   [XF] public void Should_run_10_000_tests_in_5_Millisecond()
   {
      const int one = 1;

      TimeAsserter.Execute(
         action: () => Assert.Argument.NotDefault(one),
         iterations: 10_000,
         maxTotal: 5.Milliseconds().EnvMultiply(instrumented: 6));
   }
}
