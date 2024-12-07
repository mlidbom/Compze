using Compze.SystemCE;
using Compze.Testing;
using Compze.Testing.Performance;
using NUnit.Framework;

namespace Compze.Tests.Performance.Internals.Testing.Performance;

[TestFixture] public class TimeAsserterTests : UniversalTestBase
{
   [Test] public void Execute_should_add_at_most_1_milliseconds_to_1000_iterations_of_action()
   {
      TimeAsserter.Execute(
         setup: () => {},
         tearDown: () => {},
         action: () => {},
         iterations: 1000,
         maxTotal: 1.Milliseconds()
      );
   }

   [Test] public void ExecuteThreaded_should_add_at_most_1_milliseconds_to_100_iterations_of_action()
   {
      //Warmup
      TimeAsserter.ExecuteThreaded(action: () => {}, iterations: 1000, maxTotal: 100.Milliseconds());

      TimeAsserter.ExecuteThreaded(
         action: () => {},
         iterations: 1000,
         maxTotal: 1.Milliseconds()
      );
   }
}