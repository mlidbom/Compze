using Compze.SystemCE;
using Compze.Testing.Performance;
using NUnit.Framework;
using Compze.Testing;
using Compze.Contracts.Deprecated;

namespace Compze.Tests.Contracts;

[TestFixture] public class ObjectNotDefaultPerformanceTests : UniversalTestBase
{
   [Test] public void ShouldRun300TestsIn1Milliseconds()
   {
      var one = 1;

      TimeAsserter.Execute(
         action: () => Contract.Argument(() => one).NotDefault(),
         iterations: 300,
         maxTotal: 1.Milliseconds().EnvMultiply(instrumented: 2));
   }

   [Test] public void ShouldRun300TestsIn1Millisecond()
   {
      var one = 1;

      TimeAsserter.Execute(
         action: () =>
         {
            var inspected = Contract.Argument(() => one);
            inspected.NotNullOrDefault();
         },
         iterations: 300,
         maxTotal: 1.Milliseconds().EnvMultiply(instrumented: 3));
   }
}