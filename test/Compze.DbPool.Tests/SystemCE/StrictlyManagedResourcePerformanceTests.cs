using Compze.DbPool.SystemCE;
using Compze.Internals.Testing;
using Compze.Internals.Testing.Performance;
using Compze.Tests.Infrastructure;
using Compze.xUnitBDD;

// ReSharper disable StringLiteralTypo

namespace Compze.DbPool.Tests.SystemCE;

public class StrictlyManagedResourcePerformanceTests : UniversalTestBase
{
   // ReSharper disable once ClassNeverInstantiated.Local
#pragma warning disable CA1812 // Class is never instantiated
   class StrictResource : IStrictlyManagedResource
   {
      public void Dispose() {}
   }
#pragma warning restore CA1812 // Class is never instantiated

   [XF] public void Allocated_and_disposes_XX_instances_in_50_millisecond_when_actually_collecting_stack_traces()
   {
      TimeAsserter.Execute(action: () => new StrictlyManagedResource<StrictResource>(forceStackTraceCollection: true).Dispose(),
                           iterations: 4,
                           maxTotal: 50.Milliseconds().EnvMultiply(unoptimized: 1.3));
   }

   [XF] public void Allocates_and_disposes_XX_instances_in_20_millisecond_when_not_collecting_stack_traces()
   {
      TimeAsserter.Execute(action: () => new StrictlyManagedResource<StrictResource>().Dispose(),
                           iterations: 4000,
                           maxTotal: 20.Milliseconds());
   }
}
