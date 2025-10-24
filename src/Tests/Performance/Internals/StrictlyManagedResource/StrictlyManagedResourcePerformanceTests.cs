using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Performance;
using Compze.Tests.Infrastructure;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Testing.DbPool.SystemCE;
using Compze.Utilities.Testing.XUnit.BDD;

// ReSharper disable StringLiteralTypo

namespace Compze.Tests.Performance.Internals.StrictlyManagedResource;

public class StrictlyManagedResourcePerformanceTests : UniversalTestBase
{
   // ReSharper disable once ClassNeverInstantiated.Local
#pragma warning disable CA1812 // Class is never instantiated
   class StrictResource : IStrictlyManagedResource
   {
      public void Dispose() {}
   }
#pragma warning restore CA1812 // Class is never instantiated

   [XF] public void Allocated_and_disposes_50_instances_in_200_millisecond_when_actually_collecting_stack_traces()
   {
      TimeAsserter.Execute(action: () => new StrictlyManagedResource<StrictResource>(forceStackTraceCollection: true).Dispose(),
                           iterations: 50,
                           maxTotal: 200.Milliseconds().EnvMultiply(unoptimized: 1.3));
   }

   [XF] public void Allocates_and_disposes_5000_instances_in_10_millisecond_when_not_collecting_stack_traces()
   {
      TimeAsserter.Execute(action: () => new StrictlyManagedResource<StrictResource>().Dispose(),
                           iterations: 5000,
                           maxTotal: 10.Milliseconds());
   }
}
