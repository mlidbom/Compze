using Compze.SystemCE;
using Compze.Testing.Performance;
using NUnit.Framework;
using Compze.Testing;
using Assert = Compze.Contracts.Assert;

namespace Compze.Tests.Contracts;

[TestFixture] public class ObjectNotDefaultPerformanceTests : UniversalTestBase
{
   [Test] public void Should_run_10_000_tests_in_1_Millisecond()
   {
      var one = 1;

      TimeAsserter.Execute(
         action: () => Assert.Argument.NotDefault(one),
         iterations: 10_000,
         maxTotal: 1.Milliseconds().EnvMultiply(instrumented: 6));
   }
}