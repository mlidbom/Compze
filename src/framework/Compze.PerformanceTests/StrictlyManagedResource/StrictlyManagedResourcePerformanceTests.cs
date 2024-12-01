using Compze.SystemCE;
using Compze.Testing;
using Compze.Testing.Performance;
using NUnit.Framework;

// ReSharper disable StringLiteralTypo

namespace Compze.Tests.StrictlyManagedResource;

[TestFixture]public class StrictlyManagedResourcePerformanceTests : UniversalTestBase
{
   // ReSharper disable once ClassNeverInstantiated.Local
   #pragma warning disable ca1812 // Class is never instantiated
   class StrictResource : IStrictlyManagedResource
   {
      public void Dispose() {}
   }

   [Test] public void Allocated_and_disposes_250_instances_in_40_millisecond_when_actually_collecting_stack_traces()
   {
      TimeAsserter.Execute(action: () => new StrictlyManagedResource<StrictResource>(forceStackTraceCollection: true).Dispose(),
                           iterations: 250,
                           maxTotal: 40.Milliseconds().EnvMultiply(unoptimized:1.3));
   }

   [Test] public void Allocates_and_disposes_5000_instances_in_10_millisecond_when_not_collecting_stack_traces()
   {
      TimeAsserter.Execute(action: () => new StrictlyManagedResource<StrictResource>().Dispose(),
                           iterations: 5000,
                           maxTotal: 10.Milliseconds());
   }
}