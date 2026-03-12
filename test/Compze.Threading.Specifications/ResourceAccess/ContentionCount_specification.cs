using Compze.Internals.SystemCE;
using Compze.Tests.Infrastructure;
using Compze.Threading.ResourceAccess;
using Compze.Must;
using Compze.Threading.Specifications.TestInfrastructure;
using Compze.Threading.Testing;
using Compze.xUnitBDD;
using Xunit;

namespace Compze.Threading.Specifications.ResourceAccess;

///<summary>ICriticalSection ContentionCount is tested in ICriticalSection_specification via [PCTLock]. IThreadShared is tested in IThreadShared_specification. This file covers IAwaitableCriticalSection and IAwaitableThreadShared ContentionCount.</summary>
[Collection(nameof(NonParallelCollection))]
// ReSharper disable once InconsistentNaming
public class ContentionCount_specification : UniversalTestBase
{
   readonly AwaitableLockFactory<ContentionCount_specification> _lockFactory = new();
   readonly TestingTaskRunner _runner = new(30.Seconds());

   protected override void DisposeInternal() => _lockFactory.Dispose();

   public class IAwaitableCriticalSection_ContentionCount : ContentionCount_specification
   {
      [PCTAwaitableLock] public void Is_zero_when_no_contention_occurs()
      {
         var @lock = _lockFactory.CreateAwaitableLock();

         using(@lock.TakeUpdateLock()) {}
         using(@lock.TakeReadLock()) {}

         @lock.ContentionCount.Must().Be(0L);
      }

      [PCTAwaitableLock] public void Increments_when_another_thread_contends_for_the_lock()
      {
         var @lock = _lockFactory.CreateAwaitableLock();

         var blockingLock = @lock.TakeUpdateLock();

         _runner.Run(() => { using(@lock.TakeUpdateLock()) {} });

         SpinWait.SpinUntil(() => @lock.ContentionCount >= 1, 5.Seconds()).Must().BeTrue();

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
         var monitor = IAwaitableMonitor.New(LockTimeout.Seconds(30));
         var sharedA = IAwaitableThreadShared.New(new object(), monitor);
         var sharedB = IAwaitableThreadShared.New(new object(), monitor);

         var blockingLock = monitor.TakeUpdateLock();

         using var runner = TestingTaskRunner.WithTimeout(30.Seconds());
         runner.Run(() => sharedB.Update(_ => {}));

         SpinWait.SpinUntil(() => monitor.ContentionCount >= 1, 5.Seconds()).Must().BeTrue();

         blockingLock.Dispose();

         // ReSharper disable once DisposeOnUsingVariable
         runner.Dispose();

         sharedA.Lock.ContentionCount.Must().BeGreaterThanOrEqualTo(1);
         sharedA.Lock.ContentionCount.Must().Be(sharedB.Lock.ContentionCount);
      }
   }
}
