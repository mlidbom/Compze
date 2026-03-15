using Compze.Internals.SystemCE;
using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.Threading.Interprocess;
using Compze.Threading.Specifications.ICriticalSection_.Infrastructure;
using Compze.Threading.Specifications.TestInfrastructure;
using Compze.Threading.Testing;
using Compze.xUnitBDD;
using Xunit;
using static Compze.Must.MustActions;
// ReSharper disable InconsistentNaming

// ReSharper disable AccessToDisposedClosure

namespace Compze.Threading.Specifications.IMutex_;

///<summary>IMutex-specific specifications. ICriticalSection contract behavior (mutual exclusion, reentrancy, Locked, etc.) is tested in ICriticalSection_specification via [ICriticalSectionMatrix].</summary>
[Collection(nameof(NonParallelCollection))]
public class MutexCE_specification : UniversalTestBase
{
   public class Global : MutexCE_specification
   {
      [XF] public void throws_ArgumentException_when_name_contains_backslash() =>
         Invoking(() => IMutex.Global(@"name\with\backslash")).Must().Throw<ArgumentException>();

      [XF] public void succeeds_with_a_simple_name()
      {
         using var mutex = IMutex.Global("MutexCE_specification.Global.simple_name");
         mutex.Must().NotBeNull();
      }

      [XF] public void IsGlobal_is_true()
      {
         using var mutex = IMutex.Global("MutexCE_specification.Global.IsGlobal");
         mutex.IsGlobal.Must().BeTrue();
      }

      [XF] public void Name_is_prefixed_with_Global()
      {
         using var mutex = IMutex.Global("MutexCE_specification.Global.Name");
         mutex.Name.Must().Be(@"Global\MutexCE_specification.Global.Name");
      }

      [XF] public void LockTimeout_defaults_to_LockTimeout_Default()
      {
         using var mutex = IMutex.Global("MutexCE_specification.Global.LockTimeout_default");
         mutex.LockTimeout.Must().Be(LockTimeout.Default);
      }

      [XF] public void LockTimeout_returns_the_timeout_specified_at_creation()
      {
         using var mutex = IMutex.Global("MutexCE_specification.Global.LockTimeout_custom", LockTimeout.Seconds(7));
         mutex.LockTimeout.Must().Be(LockTimeout.Seconds(7));
      }
   }

   public class LocalNamed : MutexCE_specification
   {
      [XF] public void throws_ArgumentException_when_name_contains_backslash() =>
         Invoking(() => IMutex.Local(@"name\with\backslash")).Must().Throw<ArgumentException>();

      [XF] public void succeeds_with_a_simple_name()
      {
         using var mutex = IMutex.Local("MutexCE_specification.LocalNamed.simple_name");
         mutex.Must().NotBeNull();
      }

      [XF] public void IsGlobal_is_false()
      {
         using var mutex = IMutex.Local("MutexCE_specification.LocalNamed.IsGlobal");
         mutex.IsGlobal.Must().BeFalse();
      }

      [XF] public void Name_is_prefixed_with_Local()
      {
         using var mutex = IMutex.Local("MutexCE_specification.LocalNamed.Name");
         mutex.Name.Must().Be(@"Local\MutexCE_specification.LocalNamed.Name");
      }

      [XF] public void LockTimeout_defaults_to_LockTimeout_Default()
      {
         using var mutex = IMutex.Local("MutexCE_specification.LocalNamed.LockTimeout_default");
         mutex.LockTimeout.Must().Be(LockTimeout.Default);
      }

      [XF] public void LockTimeout_returns_the_timeout_specified_at_creation()
      {
         using var mutex = IMutex.Local("MutexCE_specification.LocalNamed.LockTimeout_custom", LockTimeout.Seconds(7));
         mutex.LockTimeout.Must().Be(LockTimeout.Seconds(7));
      }
   }

   public class Locked_with_onAbandonedMutex_callback : MutexCE_specification
   {
      [XF] public void does_not_invoke_callback_when_mutex_is_not_abandoned()
      {
         var callbackInvoked = false;
         using var mutex = IMutex.Global("MutexCE_specification.onAbandoned.not_invoked", onAbandonedMutex: () => callbackInvoked = true);
         mutex.Locked(() => 0);
         callbackInvoked.Must().BeFalse();
      }

      [XF] public void invokes_callback_when_acquiring_an_abandoned_mutex()
      {
         var callbackInvoked = false;
         using var mutex = IMutex.Global("MutexCE_specification.onAbandoned.invoked", onAbandonedMutex: () => callbackInvoked = true);

         mutex.Abandon();
         mutex.TakeLock().Dispose();

         callbackInvoked.Must().BeTrue();
      }

      [XF] public void acquires_the_lock_successfully_after_abandonment()
      {
         using var mutex = IMutex.Global("MutexCE_specification.onAbandoned.acquires_lock");

         mutex.Abandon();

         var lockHandle = mutex.TakeLock();
         lockHandle.Must().NotBeNull();
         lockHandle.Dispose();
      }

      [XF] public void invokes_callback_when_mutex_is_abandoned_while_waiting_for_it()
      {
         var callbackInvoked = false;
         using var mutex = IMutex.Global("MutexCE_specification.onAbandoned.invoked_while_waiting", onAbandonedMutex: () => callbackInvoked = true);

         var triggerAbandonment = mutex.HoldLockUntilAbandoned();

         using var runner = TestingTaskRunner.WithTimeout(30.Seconds());
         var waitingTask = runner.Run(() => mutex.TakeLock().Dispose());

         SpinWait.SpinUntil(() => mutex.ContentionCount > 0, 10.Seconds());

         triggerAbandonment();
         waitingTask.Wait(10.Seconds());

         callbackInvoked.Must().BeTrue();
      }
   }

   public new class Dispose : MutexCE_specification
   {
      [XF] public void can_be_disposed_without_error()
      {
         var mutex = IMutex.Global("MutexCE_specification.Dispose.no_error");
         mutex.Dispose();
      }

      [XF] public void calling_Locked_after_Dispose_throws()
      {
         var mutex = IMutex.Global("MutexCE_specification.Dispose.Locked_after_Dispose");
         mutex.Dispose();
         Invoking(() => mutex.Locked(() => 0)).Must().Throw<ObjectDisposedException>();
      }
   }

   public class Two_IMutex_instances_with_the_same_Global_name : MutexCE_specification
   {
      [XF] public void synchronize_with_each_other()
      {
         const string name = "MutexCE_specification.SameName.synchronize";
         using var mutex1 = IMutex.Global(name);
         using var mutex2 = IMutex.Global(name);

         var insideMutex = IThreadGate.NewClosed(WaitTimeout.Seconds(30), "insideMutex");

         using var runner = TestingTaskRunner.WithTimeout(30.Seconds());
         runner.Run(
            () => mutex1.Locked(() => insideMutex.AwaitPassThrough()),
            () => mutex2.Locked(() => insideMutex.AwaitPassThrough()));

         insideMutex.TryAwaitQueueLengthEqualTo(1).Must().BeTrue();

         insideMutex.TryAwaitQueueLengthEqualTo(2, WaitTimeout.Milliseconds(50)).Must().BeFalse();

         insideMutex.Open();

         // Both threads pass through — the second was unblocked once the first released the mutex
         insideMutex.AwaitPassedThroughCountEqualTo(2);
      }
   }

   public class Two_IMutex_instances_with_the_same_Local_name : MutexCE_specification
   {
      [XF] public void synchronize_with_each_other()
      {
         const string name = "MutexCE_specification.SameName.Local.synchronize";
         using var mutex1 = IMutex.Local(name);
         using var mutex2 = IMutex.Local(name);

         var insideMutex = IThreadGate.NewClosed(WaitTimeout.Seconds(30), "insideMutex");

         using var runner = TestingTaskRunner.WithTimeout(30.Seconds());
         runner.Run(
            () => mutex1.Locked(() => insideMutex.AwaitPassThrough()),
            () => mutex2.Locked(() => insideMutex.AwaitPassThrough()));

         insideMutex.TryAwaitQueueLengthEqualTo(1).Must().BeTrue();

         insideMutex.TryAwaitQueueLengthEqualTo(2, WaitTimeout.Milliseconds(50)).Must().BeFalse();

         insideMutex.Open();

         insideMutex.AwaitPassedThroughCountEqualTo(2);
      }
   }
}
