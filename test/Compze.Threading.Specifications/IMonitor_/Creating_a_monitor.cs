using Compze.Must;
using Compze.Tests.Infrastructure.Diagnostics;
using Compze.xUnitBDD;

namespace Compze.Threading.Specifications.IMonitor_;

///<summary>Code such as an observable for each field of every object may create a great many monitors, so how much memory one costs is part of what a monitor promises.</summary>
public class Creating_a_monitor
{
   [XF] public void through_IMonitor_New_allocates_152_bytes() =>
      ManagedAllocation.BytesAllocatedBy(() => IMonitor.New()).Must().Be(152L);

   [XF] public void through_IAwaitableMonitor_New_allocates_152_bytes() =>
      ManagedAllocation.BytesAllocatedBy(() => IAwaitableMonitor.New()).Must().Be(152L);
}
