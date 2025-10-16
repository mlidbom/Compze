using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Performance;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.Infrastructure.XUnit.TestFrameworkExtensions;
using Compze.Utilities.SystemCE;
using Assert = Compze.Utilities.Contracts.Assert;

namespace Compze.Tests.Performance.Internals.XUnit.Contracts;

[Performance]
public class ObjectNotDefaultPerformanceTests : UniversalTestBase
{
   [XFact] public void Should_run_10_000_tests_in_1_Millisecond()
   {
      const int one = 1;

      TimeAsserter.Execute(
         action: () => Assert.Argument.NotDefault(one),
         iterations: 10_000,
         maxTotal: 1.Milliseconds().EnvMultiply(instrumented: 6));
   }
}