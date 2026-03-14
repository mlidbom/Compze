using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.Threading.ResourceAccess;
using Compze.Threading.Specifications.TestInfrastructure;
using Compze.xUnitBDD;
using Xunit;

// ReSharper disable InconsistentNaming

namespace Compze.Threading.Specifications.IShared_;

[Collection(nameof(NonParallelCollection))]
public class IThreadShared_specification : UniversalTestBase
{
   public class Monitor_property : IThreadShared_specification
   {
      [XF] public void exposes_the_Monitor_used_for_locking()
      {
         var shared = IThreadShared.New(new object());
         shared.Monitor.Must().NotBeNull();
      }

      [XF] public void shared_instances_with_same_Monitor_report_same_Monitor()
      {
         var monitor = IMonitor.New();
         var sharedA = IThreadShared.New(new object(), monitor);
         var sharedB = IThreadShared.New(new object(), monitor);

         sharedA.Monitor.Must().Be(sharedB.Monitor);
      }
   }
}
