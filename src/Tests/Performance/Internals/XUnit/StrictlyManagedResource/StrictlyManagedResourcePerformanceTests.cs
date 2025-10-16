using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Performance;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.Infrastructure.XUnit.TestFrameworkExtensions;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Testing.DbPool.SystemCE;

// ReSharper disable StringLiteralTypo

namespace Compze.Tests.Performance.Internals.XUnit.StrictlyManagedResource;

[Performance]
public class StrictlyManagedResourcePerformanceTests : UniversalTestBase
{
   // ReSharper disable once ClassNeverInstantiated.Local
#pragma warning disable ca1812 // Class is never instantiated
   class StrictResource : IStrictlyManagedResource
   {
      public void Dispose() {}
   }

   [XF] public void Allocated_and_disposes_250_instances_in_40_millisecond_when_actually_collecting_stack_traces()
   {
      TimeAsserter.Execute(action: () => new StrictlyManagedResource<StrictResource>(forceStackTraceCollection: true).Dispose(),
                           iterations: 250,
                           maxTotal: 350.Milliseconds().EnvMultiply(unoptimized: 1.3));
   }

   [XF] public void Allocates_and_disposes_5000_instances_in_10_millisecond_when_not_collecting_stack_traces()
   {
      TimeAsserter.Execute(action: () => new StrictlyManagedResource<StrictResource>().Dispose(),
                           iterations: 5000,
                           maxTotal: 10.Milliseconds());
   }
}
