using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;
using Compze.Tests.Infrastructure;
using Compze.Threading.ResourceAccess;
using Compze.Must;
using Compze.Threading.Specifications.TestInfrastructure;
using Compze.Threading.Testing;
using Compze.xUnitBDD;
using Xunit;

namespace Compze.Threading.Specifications.ResourceAccess;

///<summary>ILock ContentionCount is tested in ILock_specification via [PCTLock]. IThreadShared is tested in IThreadShared_specification. This file covers IAwaitableLock and IAwaitableThreadShared ContentionCount.</summary>
[Collection(nameof(NonParallelCollection))]
public class ContentionCount_specification : UniversalTestBase
{
   public class IAwaitableMonitor_ContentionCount : ContentionCount_specification
   {
      [XF] public void Is_zero_when_no_contention_occurs()
      {
         var monitor = IAwaitableLock.New();

         using(monitor.TakeUpdateLock()) {}
         using(monitor.TakeReadLock()) {}

         monitor.ContentionCount.Must().Be(0L);
      }

      [XF] public void Increments_when_another_thread_contends_for_the_lock()
      {
         var monitor = IAwaitableLock.New(LockTimeout.Seconds(30));

         var blockingLock = monitor.TakeUpdateLock();

         using var runner = TestingTaskRunner.WithTimeout(30.Seconds());
         runner.Run(() => { using(monitor.TakeUpdateLock()) {} });

         SpinWait.SpinUntil(() => monitor.ContentionCount >= 1, 5.Seconds()).Must().BeTrue();

         blockingLock.Dispose();
      }
   }

   public class IAwaitableThreadShared_exposes_Monitor : ContentionCount_specification
   {
      [XF] public void ContentionCount_is_accessible_through_Monitor_property()
      {
         var shared = IAwaitableThreadShared.New(new object());

         shared.Update(_ => {});

         shared.Lock.ContentionCount.Must().Be(0L);
      }

      [XF] public void Shared_instances_with_same_monitor_share_contention_tracking()
      {
         var monitor = IAwaitableLock.New(LockTimeout.Seconds(30));
         var sharedA = IAwaitableThreadShared.New(new object(), monitor);
         var sharedB = IAwaitableThreadShared.New(new object(), monitor);

         var blockingLock = monitor.TakeUpdateLock();

         using var runner = TestingTaskRunner.WithTimeout(30.Seconds());
         runner.Run(() => sharedB.Update(_ => {}));

         SpinWait.SpinUntil(() => monitor.ContentionCount >= 1, 5.Seconds()).Must().BeTrue();

         blockingLock.Dispose();

         runner.Dispose();

         sharedA.Lock.ContentionCount.Must().BeGreaterThanOrEqualTo(1);
         sharedA.Lock.ContentionCount.Must().Be(sharedB.Lock.ContentionCount);
      }
   }
}
