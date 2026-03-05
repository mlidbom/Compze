using Compze.Tessaging.Hosting.Testing.Performance;
using Compze.Tests.Infrastructure;
using Compze.Internals.SystemCE;
using Compze.xUnitBDD;

namespace Compze.Tests.Performance.Internals.Testing.Performance;

public class TimeAsserterTests : UniversalTestBase
{
   [XF] public void Execute_should_add_at_most_1_milliseconds_to_1000_iterations_of_action()
   {
      TimeAsserter.Execute(
         setup: () => {},
         tearDown: () => {},
         action: () => {},
         iterations: 1000,
         maxTotal: 1.Milliseconds()
      );
   }

   [XF] public void ExecuteThreaded_should_add_at_most_4_milliseconds_to_100_iterations_of_action()
   {
      //Warmup
      TimeAsserter.ExecuteThreaded(action: () => {}, iterations: 1000, maxTotal: 100.Milliseconds());

      TimeAsserter.ExecuteThreaded(
         action: () => {},
         iterations: 1000,
         maxTotal: 4.Milliseconds()
      );
   }
}
