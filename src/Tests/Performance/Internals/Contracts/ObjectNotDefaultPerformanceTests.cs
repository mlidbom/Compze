
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Performance;
using Compze.Utilities.SystemCE;
using NUnit.Framework;
using Assert = Compze.Utilities.Contracts.Assert;
using Compze.Tests.Infrastructure.NUnit;

namespace Compze.Tests.Performance.Internals.Contracts;

[TestFixture] public class ObjectNotDefaultPerformanceTests : NUnitTestBase
{
   [Test] public void Should_run_10_000_tests_in_1_Millisecond()
   {
      const int one = 1;

      TimeAsserter.Execute(
         action: () => Assert.Argument.NotDefault(one),
         iterations: 10_000,
         maxTotal: 1.Milliseconds().EnvMultiply(instrumented: 6));
   }
}