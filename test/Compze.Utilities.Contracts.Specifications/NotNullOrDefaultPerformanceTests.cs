using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Performance;
using Compze.Tests.Infrastructure;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Testing.XUnit.BDD;
using Assert = Compze.Utilities.Contracts.Assert;

namespace Compze.Utilities.Contracts.Specifications;

public class NotNullOrDefaultPerformanceTests : UniversalTestBase
{
   [XF] public void Should_run_10_000_tests_in_3_Millisecond()
   {
      int? notNullOrDefault = 1;

      TimeAsserter.Execute(
         action: () => Assert.Argument.NotNullOrDefault(notNullOrDefault),
         iterations: 10_000,
         maxTotal: 3.Milliseconds().EnvMultiply(instrumented: 6.0)
      );
   }
}
