using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;
using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.Threading.Interprocess;
using Compze.Threading.Specifications.TestInfrastructure;
using Compze.Threading.Testing;
using Compze.xUnitBDD;
using Xunit;
using static Compze.Must.MustActions;

// ReSharper disable AccessToDisposedClosure

namespace Compze.Threading.Specifications.Interprocess;

///<summary>IMutex-specific specifications. ILock contract behavior (mutual exclusion, reentrancy, Locked, etc.) is tested in ILock_specification via [PCTLock].</summary>
[Collection(nameof(NonParallelCollection))]
public class MutexCE_specification : UniversalTestBase
{
   public class GlobalNamed : MutexCE_specification
   {
      [XF] public void throws_ArgumentException_when_name_contains_backslash() =>
         Invoking(() => IMutex.GlobalNamed(@"name\with\backslash")).Must().Throw<ArgumentException>();

      [XF] public void succeeds_with_a_simple_name()
      {
         using var mutex = IMutex.GlobalNamed("MutexCE_specification.GlobalNamed.simple_name");
         mutex.Must().NotBeNull();
      }
   }

   public class LocalNamed : MutexCE_specification
   {
      [XF] public void throws_ArgumentException_when_name_contains_backslash() =>
         Invoking(() => IMutex.LocalNamed(@"name\with\backslash")).Must().Throw<ArgumentException>();

      [XF] public void succeeds_with_a_simple_name()
      {
         using var mutex = IMutex.LocalNamed("MutexCE_specification.LocalNamed.simple_name");
         mutex.Must().NotBeNull();
      }
   }

   public class Locked_with_onAbandonedMutex_callback : MutexCE_specification
   {
      [XF] public void does_not_invoke_callback_when_mutex_is_not_abandoned()
      {
         var callbackInvoked = false;
         using var mutex = IMutex.GlobalNamed("MutexCE_specification.onAbandoned.not_invoked", onAbandonedMutex: () => callbackInvoked = true);
         mutex.Locked(() => 0);
         callbackInvoked.Must().BeFalse();
      }
   }

   public new class Dispose : MutexCE_specification
   {
      [XF] public void can_be_disposed_without_error()
      {
         var mutex = IMutex.GlobalNamed("MutexCE_specification.Dispose.no_error");
         mutex.Dispose();
      }

      [XF] public void calling_Locked_after_Dispose_throws()
      {
         var mutex = IMutex.GlobalNamed("MutexCE_specification.Dispose.Locked_after_Dispose");
         mutex.Dispose();
         Invoking(() => mutex.Locked(() => 0)).Must().Throw<ObjectDisposedException>();
      }
   }

   public class Two_IMutex_instances_with_the_same_GlobalNamed_name : MutexCE_specification
   {
      [XF] public void synchronize_with_each_other()
      {
         const string name = "MutexCE_specification.SameName.synchronize";
         using var mutex1 = IMutex.GlobalNamed(name);
         using var mutex2 = IMutex.GlobalNamed(name);

         var insideLockSection = GatedCodeSection.Closed(WaitTimeout.Seconds(30), "insideLock");

         using var runner = TestingTaskRunner.WithTimeout(30.Seconds());
         runner.Run(
            () => mutex1.Locked(() => insideLockSection.Enter().Dispose()),
            () => mutex2.Locked(() => insideLockSection.Enter().Dispose()));

         insideLockSection.LetOneThreadEnterAndReachExit();
         insideLockSection.EntranceGate.TryAwaitQueueLengthEqualTo(1, WaitTimeout.Milliseconds(50)).Must().BeFalse();
         insideLockSection.Open();
      }
   }
}
