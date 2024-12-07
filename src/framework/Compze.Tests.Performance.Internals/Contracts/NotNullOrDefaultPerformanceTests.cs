using Compze.SystemCE;
using Compze.Testing;
using Compze.Testing.Performance;
using NUnit.Framework;
using Assert = Compze.Contracts.Assert;

namespace Compze.Tests.Performance.Internals.Contracts;

[TestFixture] public class NotNullOrDefaultPerformanceTests : UniversalTestBase
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
