using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Performance;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Testing.XUnit.BDD;
using Assert = Compze.Utilities.Contracts.Assert;

namespace Compze.Tests.Performance.Internals.Contracts;

public class NotNullOrDefaultPerformanceTests : UniversalTestBase
{
   [XF] public void Should_run_10_000_tests_in_2_Millisecond()
   {
      int? notNullOrDefault = 1;

      TimeAsserter.Execute(
         action: () => Assert.Argument.NotNullOrDefault(notNullOrDefault),
         iterations: 10_000,
         maxTotal: 2.Milliseconds().EnvMultiply(instrumented: 6.0)
      );
   }
}
