using Composable.Contracts;
using Composable.SystemCE;
using Composable.Testing.Performance;
using NUnit.Framework;
using Composable.Testing;

namespace Composable.Tests.Contracts;

[TestFixture] public class LambdaBasedArgumentSpecsPerformanceTests : UniversalTestBase
{
   [Test] public void ShouldRun10_000TestsIn30Millisecond() //The expression compilation stuff was worrying but this should be OK except for tight loops.
   {
      var notNullOrDefault = new object();

      TimeAsserter.Execute(
         action: () => Contract.Argument(() => notNullOrDefault).NotNullOrDefault(),
         iterations: 10_000,
         maxTotal: 30.Milliseconds().EnvMultiply(instrumented: 6.0)
      );
   }
}