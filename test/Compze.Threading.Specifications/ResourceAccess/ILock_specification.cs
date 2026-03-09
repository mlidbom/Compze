using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;
using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.Threading.Testing;
using Xunit;
using static Compze.Must.MustActions;

// ReSharper disable AccessToDisposedClosure

namespace Compze.Threading.Specifications.ResourceAccess;

[Collection(nameof(NonParallelCollection))]
public class ILock_specification : UniversalTestBase
{
   readonly LockFactory<ILock_specification> _lockFactory = new();

   protected override void DisposeInternal() => _lockFactory.Dispose();

   public class Locked_with_Func : ILock_specification
   {
      [PCTLock] public void returns_the_value_from_the_function()
      {
         var @lock = _lockFactory.CreateLock();
         @lock.Locked(() => 42).Must().Be(42);
      }

      [PCTLock] public void propagates_exceptions_from_the_function()
      {
         var @lock = _lockFactory.CreateLock();
         Invoking(() => @lock.Locked<int>(() => throw new InvalidOperationException("test")))
           .Must().Throw<InvalidOperationException>()
           .Which.Message.Must().Be("test");
      }
   }

   public class Locked_with_Action : ILock_specification
   {
      [PCTLock] public void executes_the_action()
      {
         var @lock = _lockFactory.CreateLock();
         var executed = false;
         @lock.Locked(() => executed = true);
         executed.Must().BeTrue();
      }
   }

   public class TakeLock : ILock_specification
   {
      [PCTLock] public void provides_mutual_exclusion_across_threads()
      {
         var @lock = _lockFactory.CreateLock(LockTimeout.Seconds(30));
         var insideLockSection = GatedCodeSection.Closed(WaitTimeout.Seconds(30), "insideLock");

         using var runner = TestingTaskRunner.WithTimeout(30.Seconds());
         runner.Run(
            () => @lock.Locked(() => insideLockSection.Enter().Dispose()),
            () => @lock.Locked(() => insideLockSection.Enter().Dispose()));

         insideLockSection.LetOneThreadEnterAndReachExit();
         insideLockSection.EntranceGate.TryAwaitQueueLengthEqualTo(1, WaitTimeout.Milliseconds(50)).Must().BeFalse();
         insideLockSection.Open();
      }

      [PCTLock] public void supports_reentrant_locking_from_the_same_thread()
      {
         var @lock = _lockFactory.CreateLock();
         var result = @lock.Locked(() => @lock.Locked(() => 42));
         result.Must().Be(42);
      }

      [PCTLock] public void releases_the_lock_even_when_the_function_throws()
      {
         var @lock = _lockFactory.CreateLock(LockTimeout.Seconds(30));

         try { @lock.Locked<int>(() => throw new InvalidOperationException()); }
         catch(InvalidOperationException) { }

         var secondThreadGotLock = ThreadGate.Open(WaitTimeout.Seconds(30), "secondThreadGotLock");

         using var runner = TestingTaskRunner.WithTimeout(30.Seconds());
         runner.Run(() => @lock.Locked(() => secondThreadGotLock.AwaitPassThrough()));

         secondThreadGotLock.AwaitPassedThroughCountEqualTo(1);
      }
   }

   public class ContentionCount : ILock_specification
   {
      [PCTLock] public void is_zero_when_no_contention_occurs()
      {
         var @lock = _lockFactory.CreateLock();

         using(@lock.TakeLock()) {}
         using(@lock.TakeLock()) {}

         @lock.ContentionCount.Must().Be(0L);
      }

      [PCTLock] public void increments_when_another_thread_contends_for_the_lock()
      {
         var @lock = _lockFactory.CreateLock(LockTimeout.Seconds(5));
         using var contenderIsWaiting = new ManualResetEventSlim(false);

         var blockingLock = @lock.TakeLock();
         var contenderTask = TaskCE.Run(() =>
         {
            contenderIsWaiting.Set();
            using(@lock.TakeLock()) {}
         });

         contenderIsWaiting.Wait();
         Thread.Sleep(50.Milliseconds());
         blockingLock.Dispose();

         Task.WaitAll(contenderTask);

         @lock.ContentionCount.Must().BeGreaterThanOrEqualTo(1);
      }
   }
}
