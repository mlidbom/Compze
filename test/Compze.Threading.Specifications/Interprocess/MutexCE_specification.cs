using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;
using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.Threading.Interprocess;
using Compze.Threading.Testing;
using Compze.xUnitBDD;
using Xunit;
using static Compze.Must.MustActions;

// ReSharper disable AccessToDisposedClosure

namespace Compze.Threading.Specifications.Interprocess;

[Collection(nameof(NonParallelCollection))]
public class MutexCE_specification : UniversalTestBase
{
   public class GlobalNamed : MutexCE_specification
   {
      [XF] public void throws_ArgumentException_when_name_contains_backslash() =>
         Invoking(() => MutexCE.GlobalNamed(@"name\with\backslash")).Must().Throw<ArgumentException>();

      [XF] public void succeeds_with_a_simple_name()
      {
         using var mutex = MutexCE.GlobalNamed("MutexCE_specification.GlobalNamed.simple_name");
         mutex.Must().NotBeNull();
      }
   }

   public class LocalNamed : MutexCE_specification
   {
      [XF] public void throws_ArgumentException_when_name_contains_backslash() =>
         Invoking(() => MutexCE.LocalNamed(@"name\with\backslash")).Must().Throw<ArgumentException>();

      [XF] public void succeeds_with_a_simple_name()
      {
         using var mutex = MutexCE.LocalNamed("MutexCE_specification.LocalNamed.simple_name");
         mutex.Must().NotBeNull();
      }
   }

   public class Locked_with_Func : MutexCE_specification
   {
      [XF] public void returns_the_value_from_the_function()
      {
         using var mutex = MutexCE.GlobalNamed("MutexCE_specification.Locked_with_Func.returns_value");
         mutex.Locked(() => 42).Must().Be(42);
      }

      [XF] public void provides_mutual_exclusion_across_threads()
      {
         using var mutex = MutexCE.GlobalNamed("MutexCE_specification.Locked_with_Func.mutual_exclusion");
         var insideLockSection = GatedCodeSection.Closed(WaitTimeout.Seconds(30), "insideLock");

         using var runner = TestingTaskRunner.WithTimeout(30.Seconds());
         runner.Run(
            () => mutex.Locked(() => insideLockSection.Enter().Dispose()),
            () => mutex.Locked(() => insideLockSection.Enter().Dispose()));

         insideLockSection.LetOneThreadEnterAndReachExit();
         insideLockSection.EntranceGate.TryAwaitQueueLengthEqualTo(1, WaitTimeout.Milliseconds(50)).Must().BeFalse();
         insideLockSection.Open();
      }

      [XF] public void supports_reentrant_locking_from_the_same_thread()
      {
         using var mutex = MutexCE.GlobalNamed("MutexCE_specification.Locked_with_Func.reentrant");
         var result = mutex.Locked(() => mutex.Locked(() => 42));
         result.Must().Be(42);
      }

      [XF] public void propagates_exceptions_from_the_function()
      {
         using var mutex = MutexCE.GlobalNamed("MutexCE_specification.Locked_with_Func.propagates_exceptions");
         Invoking(() => mutex.Locked<int>(() => throw new InvalidOperationException("test")))
           .Must().Throw<InvalidOperationException>()
           .Which.Message.Must().Be("test");
      }

      [XF] public void releases_the_mutex_even_when_the_function_throws()
      {
         using var mutex = MutexCE.GlobalNamed("MutexCE_specification.Locked_with_Func.releases_on_throw");

         try { mutex.Locked<int>(() => throw new InvalidOperationException()); }
         catch(InvalidOperationException) { }

         var secondThreadGotLock = ThreadGate.Open(WaitTimeout.Seconds(30), "secondThreadGotLock");

         using var runner = TestingTaskRunner.WithTimeout(30.Seconds());
         runner.Run(() => mutex.Locked(() => secondThreadGotLock.AwaitPassThrough()));

         secondThreadGotLock.AwaitPassedThroughCountEqualTo(1);
      }
   }

   public class Locked_with_Action : MutexCE_specification
   {
      [XF] public void executes_the_action()
      {
         using var mutex = MutexCE.GlobalNamed("MutexCE_specification.Locked_with_Action.executes");
         var executed = false;
         mutex.Locked(() => executed = true);
         executed.Must().BeTrue();
      }
   }

   public class Locked_with_onAbandonedMutex_callback : MutexCE_specification
   {
      [XF] public void does_not_invoke_callback_when_mutex_is_not_abandoned()
      {
         using var mutex = MutexCE.GlobalNamed("MutexCE_specification.onAbandoned.not_invoked");
         var callbackInvoked = false;
         mutex.Locked(() => 0, onAbandonedMutex: () => callbackInvoked = true);
         callbackInvoked.Must().BeFalse();
      }
   }

   public new class Dispose : MutexCE_specification
   {
      [XF] public void can_be_disposed_without_error()
      {
         var mutex = MutexCE.GlobalNamed("MutexCE_specification.Dispose.no_error");
         mutex.Dispose();
      }

      [XF] public void calling_Locked_after_Dispose_throws()
      {
         var mutex = MutexCE.GlobalNamed("MutexCE_specification.Dispose.Locked_after_Dispose");
         mutex.Dispose();
         Invoking(() => mutex.Locked(() => 0)).Must().Throw<ObjectDisposedException>();
      }
   }

   public class Two_MutexCE_instances_with_the_same_GlobalNamed_name : MutexCE_specification
   {
      [XF] public void synchronize_with_each_other()
      {
         const string name = "MutexCE_specification.SameName.synchronize";
         using var mutex1 = MutexCE.GlobalNamed(name);
         using var mutex2 = MutexCE.GlobalNamed(name);

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
