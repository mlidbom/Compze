using Compze.SystemCE.ThreadingCE.TasksCE;
using Compze.Tests.Infrastructure;
using Compze.Utilities.SystemCE;
using Compze.Threading.ResourceAccess;
using Compze.Must;
using Compze.xUnit.BDD;
using Xunit;
// ReSharper disable AccessToDisposedClosure

namespace Compze.Threading.Specifications.ResourceAccess;

[Collection(nameof(NonParallelCollection))]
public class ContentionCount_specification : UniversalTestBase
{
   public class IMonitor_ContentionCount : ContentionCount_specification
   {
      [XF] public void Is_zero_when_no_contention_occurs()
      {
         var monitor = IMonitor.New();

         using(monitor.TakeLock()) {}
         using(monitor.TakeLock()) {}

         monitor.ContentionCount.Must().Be(0L);
      }

      [XF] public void Increments_when_another_thread_contends_for_the_lock()
      {
         var monitor = IMonitor.New(LockTimeout.Seconds(5));
         using var blockerHoldsLock = new ManualResetEventSlim(false);
         using var contenderIsWaiting = new ManualResetEventSlim(false);

         var blockingLock = monitor.TakeLock();
         var contenderTask = TaskCE.Run(() =>
         {
            contenderIsWaiting.Set();
            using(monitor.TakeLock()) {}
         });

         contenderIsWaiting.Wait();
         Thread.Sleep(50.Milliseconds());
         blockingLock.Dispose();

         Task.WaitAll(contenderTask);

         monitor.ContentionCount.Must().BeGreaterThanOrEqualTo(1);
      }
   }

   public class IAwaitableMonitor_ContentionCount : ContentionCount_specification
   {
      [XF] public void Is_zero_when_no_contention_occurs()
      {
         var monitor = IAwaitableMonitor.New();

         using(monitor.TakeUpdateLock()) {}
         using(monitor.TakeReadLock()) {}

         monitor.ContentionCount.Must().Be(0L);
      }

      [XF] public void Increments_when_another_thread_contends_for_the_lock()
      {
         var monitor = IAwaitableMonitor.New(LockTimeout.Seconds(5));
         using var contenderIsWaiting = new ManualResetEventSlim(false);

         var blockingLock = monitor.TakeUpdateLock();
         var contenderTask = TaskCE.Run(() =>
         {
            contenderIsWaiting.Set();
            using(monitor.TakeUpdateLock()) {}
         });

         contenderIsWaiting.Wait();
         Thread.Sleep(50.Milliseconds());
         blockingLock.Dispose();

         Task.WaitAll(contenderTask);

         monitor.ContentionCount.Must().BeGreaterThanOrEqualTo(1);
      }
   }

   public class IThreadShared_exposes_Monitor : ContentionCount_specification
   {
      [XF] public void ContentionCount_is_accessible_through_Monitor_property()
      {
         var shared = IThreadShared.New(new object());

         using(shared.Monitor.TakeLock()) {}

         shared.Monitor.ContentionCount.Must().Be(0L);
      }

      [XF] public void Shared_instances_with_same_monitor_report_same_ContentionCount()
      {
         var monitor = IMonitor.New();
         var sharedA = IThreadShared.New(new object(), monitor);
         var sharedB = IThreadShared.New(new object(), monitor);

         sharedA.Monitor.Must().Be(sharedB.Monitor);
      }
   }

   public class IAwaitableThreadShared_exposes_Monitor : ContentionCount_specification
   {
      [XF] public void ContentionCount_is_accessible_through_Monitor_property()
      {
         var shared = IAwaitableThreadShared.New(new object());

         shared.Update(_ => {});

         shared.Monitor.ContentionCount.Must().Be(0L);
      }

      [XF] public void Shared_instances_with_same_monitor_share_contention_tracking()
      {
         var monitor = IAwaitableMonitor.New(LockTimeout.Seconds(5));
         var sharedA = IAwaitableThreadShared.New(new object(), monitor);
         var sharedB = IAwaitableThreadShared.New(new object(), monitor);
         using var contenderIsWaiting = new ManualResetEventSlim(false);

         var blockingLock = monitor.TakeUpdateLock();
         var contenderTask = TaskCE.Run(() =>
         {
            contenderIsWaiting.Set();
            sharedB.Update(_ => {});
         });

         contenderIsWaiting.Wait();
         Thread.Sleep(50.Milliseconds());
         blockingLock.Dispose();

         Task.WaitAll(contenderTask);

         sharedA.Monitor.ContentionCount.Must().BeGreaterThanOrEqualTo(1);
         sharedA.Monitor.ContentionCount.Must().Be(sharedB.Monitor.ContentionCount);
      }
   }
}
