using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;
using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.Threading.Specifications.TestInfrastructure;
using Compze.Threading.Testing;
using Xunit;
using static Compze.Must.MustActions;

// ReSharper disable AccessToDisposedClosure

namespace Compze.Threading.Specifications;

[Collection(nameof(NonParallelCollection))]
public class ILock_specification : UniversalTestBase
{
   readonly LockFactory<ILock_specification> _lockFactory = new();
   readonly TestingTaskRunner _runner = new(timeout:30.Seconds());

   protected override void DisposeInternal()
   {
      _runner.Dispose();
      _lockFactory.Dispose();
   }

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

      [PCTLock] public void propagates_exceptions_from_the_function()
      {
         var @lock = _lockFactory.CreateLock();
         Invoking(() => @lock.Locked(() => throw new InvalidOperationException("test")))
           .Must().Throw<InvalidOperationException>()
           .Which.Message.Must().Be("test");
      }
   }

   public class TakeLock : ILock_specification
   {
      [PCTLock] public void provides_mutual_exclusion_across_threads()
      {
         var @lock = _lockFactory.CreateLock(LockTimeout.Seconds(30));
         var insideLockGate = ThreadGate.Closed(WaitTimeout.Seconds(30), "insideLock");

         _runner.Run(
            () => @lock.Locked(() => insideLockGate.AwaitPassThrough()),
            () => @lock.Locked(() => insideLockGate.AwaitPassThrough()));

         insideLockGate.AwaitQueueLengthEqualTo(1);
         insideLockGate.TryAwaitQueueLengthEqualTo(2, WaitTimeout.Milliseconds(50)).Must().BeFalse();
         insideLockGate.Open();
         insideLockGate.AwaitPassedThroughCountEqualTo(2);
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

         TaskCE.Run(() => @lock.Locked(() => {})).Wait();
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
         var @lock = _lockFactory.CreateLock(LockTimeout.Seconds(30));

         var blockingLock = @lock.TakeLock();

         _runner.Run(() => { using(@lock.TakeLock()) {} });

         SpinWait.SpinUntil(() => @lock.ContentionCount >= 1, 5.Seconds()).Must().BeTrue();

         blockingLock.Dispose();
      }
   }
}
