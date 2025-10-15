
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Performance;
using Compze.Utilities.SystemCE;
using NUnit.Framework;
using Assert = Compze.Utilities.Contracts.Assert;
using Compze.Tests.Infrastructure.NUnit;

namespace Compze.Tests.Performance.Internals.Contracts;

[TestFixture] public class NotNullOrDefaultPerformanceTests : NUnitTestBase
{
   [Test] public void Should_run_10_000_tests_in_2_Millisecond()
   {
      int? notNullOrDefault = 1;

      TimeAsserter.Execute(
         action: () => Assert.Argument.NotNullOrDefault(notNullOrDefault),
         iterations: 10_000,
         maxTotal: 2.Milliseconds().EnvMultiply(instrumented: 6.0)
      );
   }
}
