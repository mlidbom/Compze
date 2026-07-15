using Compze.Internals.SystemCE;
using Compze.Must;

using Compze.Tests.Infrastructure;
using Compze.Threading.Exceptions;
using Compze.Threading.ResourceAccess;
using Compze.Threading.Specifications.IAwaitableShared_.Infrastructure;
using Compze.Threading.Specifications.TestInfrastructure;
using Compze.Threading.Testing;
using Xunit;
using static Compze.Must.MustActions;
// ReSharper disable InconsistentNaming

namespace Compze.Threading.Specifications.IAwaitableShared_;

[Collection(nameof(NonParallelCollection))]
public class IAwaitableShared_specification : UniversalTestBase
{
   readonly IAwaitableSharedMatrixAttribute.Factory<IAwaitableShared_specification> _factory = new();
   readonly TestingTaskRunner _runner = TestingTaskRunner.WithTimeout(30.Seconds());

   protected override void DisposeInternal()
   {
      _runner.Dispose();
      _factory.Dispose();
   }

   public class Read_with_Func : IAwaitableShared_specification
   {
      [IAwaitableSharedMatrix] public void returns_the_value_from_the_function()
      {
         var shared = _factory.Create(new SharedTestValue { Value = 42 });
         shared.Read(v => v.Value).Must().Be(42);
      }

      [IAwaitableSharedMatrix] public void provides_the_shared_value_to_the_function()
      {
         var shared = _factory.Create(new SharedTestValue { Items = [1, 2, 3, 4, 5] });
         shared.Read(v => v.Items.Count).Must().Be(5);
      }
   }

   public class Read_with_Action : IAwaitableShared_specification
   {
      [IAwaitableSharedMatrix] public void executes_the_action_with_the_shared_value()
      {
         var shared = _factory.Create(new SharedTestValue());
         shared.Read(v => v.Items.Count);
         shared.Read(v => v.Items.Count).Must().Be(0);
      }
   }

   public class Update_with_Func : IAwaitableShared_specification
   {
      [IAwaitableSharedMatrix] public void returns_the_value_from_the_function()
      {
         var shared = _factory.Create(new SharedTestValue { Value = 42 });
         shared.Update(v => v.Value + 1).Must().Be(43);
      }

      [IAwaitableSharedMatrix] public void provides_the_shared_value_to_the_function()
      {
         var shared = _factory.Create(new SharedTestValue { Items = [1, 2, 3, 4, 5] });
         shared.Update(v => v.Items.Count).Must().Be(5);
      }
   }

   public class Update_with_Action : IAwaitableShared_specification
   {
      [IAwaitableSharedMatrix] public void executes_the_action_with_the_shared_value()
      {
         var shared = _factory.Create(new SharedTestValue());
         shared.Update(v => v.Items.Add(42));
         shared.Read(v => v.Items.Count).Must().Be(1);
      }
   }

   static void AssertAllAccessMethodsFail(IAwaitableShared<SharedTestValue> shared)
   {
      Invoking(() => shared.Read(_ => 0, timeout: LockTimeout.Milliseconds(10)))
        .Must().Throw<TakeLockTimeoutException>();

      Invoking(() => shared.Read(_ => {}, timeout: LockTimeout.Milliseconds(10)))
        .Must().Throw<TakeLockTimeoutException>();

      Invoking(() => shared.Update(_ => 0, timeout: LockTimeout.Milliseconds(10)))
        .Must().Throw<TakeLockTimeoutException>();

      Invoking(() => shared.Update(_ => {}, timeout: LockTimeout.Milliseconds(10)))
        .Must().Throw<TakeLockTimeoutException>();
   }

   static void AssertAllAccessMethodsSucceed(IAwaitableShared<SharedTestValue> shared)
   {
      shared.Read(_ => 0, timeout: LockTimeout.Seconds(10));
      shared.Read(_ => {}, timeout: LockTimeout.Seconds(10));
      shared.Update(_ => 0, timeout: LockTimeout.Seconds(10));
      shared.Update(_ => {}, timeout: LockTimeout.Seconds(10));
   }

   static void AssertAllAccessMethodsFailsWhileGateIsClosedAllSucceedAfterOpeningGate(IAwaitableShared<SharedTestValue> shared, IThreadGate insideLock)
   {
      AssertAllAccessMethodsFail(shared);
      insideLock.Open();
      insideLock.AwaitPassedThroughCountEqualTo(1);
      AssertAllAccessMethodsSucceed(shared);
   }

   public class No_other_thread_can_access_the_the_shared_data : IAwaitableShared_specification
   {
      void AssertAccessMethodExcludesAllOtherAccess(Action<IAwaitableShared<SharedTestValue>, IThreadGate> accessMethodUnderTest)
      {
         var shared = _factory.Create(new SharedTestValue(), LockTimeout.Seconds(30));
         var insideLock = IThreadGate.NewClosed(WaitTimeout.Seconds(30), "insideLock");

         _runner.Run(() => accessMethodUnderTest(shared, insideLock));
         insideLock.AwaitQueueLengthEqualTo(1);

         AssertAllAccessMethodsFailsWhileGateIsClosedAllSucceedAfterOpeningGate(shared, insideLock);
      }

      [IAwaitableSharedMatrix] public void while_Read_Func_holds_the_lock() =>
         AssertAccessMethodExcludesAllOtherAccess((shared, gate) => shared.Read(_ => gate.AwaitPassThrough()));

      [IAwaitableSharedMatrix] public void while_Read_Action_holds_the_lock() =>
         AssertAccessMethodExcludesAllOtherAccess((shared, gate) => shared.Read(_ => gate.AwaitPassThrough()));

      [IAwaitableSharedMatrix] public void while_Update_Func_holds_the_lock() =>
         AssertAccessMethodExcludesAllOtherAccess((shared, gate) => shared.Update(_ => gate.AwaitPassThrough()));

      [IAwaitableSharedMatrix] public void while_Update_Action_holds_the_lock() =>
         AssertAccessMethodExcludesAllOtherAccess((shared, gate) => shared.Update(_ => gate.AwaitPassThrough()));

      [IAwaitableSharedMatrix] public void while_TryReadWhen_holds_the_lock() =>
         AssertAccessMethodExcludesAllOtherAccess((shared, gate) => shared.TryReadWhen(_ => true, _ => gate.AwaitPassThrough(), out _));
   }

   public class ReadWhen : IAwaitableShared_specification
   {
      [IAwaitableSharedMatrix] public void returns_value_when_condition_is_immediately_true()
      {
         var shared = _factory.Create(new SharedTestValue { Value = 42 });
         shared.ReadWhen(_ => true, v => v.Value).Must().Be(42);
      }

      [IAwaitableSharedMatrix] public void waits_until_condition_becomes_true_then_returns_value()
      {
         var shared = _factory.Create(new SharedTestValue(), LockTimeout.Seconds(30), WaitTimeout.Seconds(30));
         var readCompleted = IThreadGate.NewOpen(WaitTimeout.Seconds(10), "readCompleted");

         _runner.Run(() =>
         {
            shared.ReadWhen(v => v.Items.Count > 0, v => v.Items[0]).Must().Be(99);
            readCompleted.AwaitPassThrough();
         });

         readCompleted.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Milliseconds(100)).Must().BeFalse();
         shared.Update(v => v.Items.Add(99));
         readCompleted.AwaitPassedThroughCountEqualTo(1);
      }

      [IAwaitableSharedMatrix] public void throws_AwaitingConditionTimeoutException_when_condition_never_becomes_true()
      {
         var shared = _factory.Create(new SharedTestValue(), waitTimeout: WaitTimeout.Milliseconds(100));
         Invoking(() => shared.ReadWhen(_ => false, v => v.Value))
           .Must().Throw<AwaitingConditionTimeoutException>();
      }
   }

   public class TryReadWhen : IAwaitableShared_specification
   {
      [IAwaitableSharedMatrix] public void returns_true_and_the_value_when_condition_is_immediately_true()
      {
         var shared = _factory.Create(new SharedTestValue { Value = 42 });
         shared.TryReadWhen(_ => true, v => v.Value, out var result).Must().BeTrue();
         result.Must().Be(42);
      }

      [IAwaitableSharedMatrix] public void returns_true_and_the_value_when_condition_becomes_true_within_timeout()
      {
         var shared = _factory.Create(new SharedTestValue(), LockTimeout.Seconds(30), WaitTimeout.Seconds(30));
         var readCompleted = IThreadGate.NewOpen(WaitTimeout.Seconds(10), "readCompleted");

         _runner.Run(() =>
         {
            shared.TryReadWhen(v => v.Items.Count > 0, v => v.Items[0], out var value).Must().BeTrue();
            value.Must().Be(99);
            readCompleted.AwaitPassThrough();
         });

         readCompleted.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Milliseconds(100)).Must().BeFalse();
         shared.Update(v => v.Items.Add(99));
         readCompleted.AwaitPassedThroughCountEqualTo(1);
      }

      [IAwaitableSharedMatrix] public void returns_false_and_default_when_condition_never_becomes_true_within_timeout()
      {
         var shared = _factory.Create(new SharedTestValue(), waitTimeout: WaitTimeout.Milliseconds(100));
         shared.TryReadWhen(_ => false, v => v.Value, out var result).Must().BeFalse();
         result.Must().Be(0);
      }
   }

   public class UpdateWhen_with_Func : IAwaitableShared_specification
   {
      [IAwaitableSharedMatrix] public void returns_value_when_condition_is_immediately_true()
      {
         var shared = _factory.Create(new SharedTestValue { Value = 42 });
         shared.UpdateWhen(_ => true, v => v.Value + 1).Must().Be(43);
      }

      [IAwaitableSharedMatrix] public void waits_until_condition_becomes_true_then_executes_update()
      {
         var shared = _factory.Create(new SharedTestValue(), LockTimeout.Seconds(30), WaitTimeout.Seconds(30));
         var updateCompleted = IThreadGate.NewOpen(WaitTimeout.Seconds(10), "updateCompleted");

         _runner.Run(() =>
         {
            shared.UpdateWhen(v => v.Items.Count > 0, v => v.Items[0]);
            updateCompleted.AwaitPassThrough();
         });

         updateCompleted.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Milliseconds(100)).Must().BeFalse();
         shared.Update(v => v.Items.Add(42));
         updateCompleted.AwaitPassedThroughCountEqualTo(1);
      }

      [IAwaitableSharedMatrix] public void throws_AwaitingConditionTimeoutException_when_condition_never_becomes_true()
      {
         var shared = _factory.Create(new SharedTestValue(), waitTimeout: WaitTimeout.Milliseconds(100));
         Invoking(() => shared.UpdateWhen(_ => false, v => v.Value))
           .Must().Throw<AwaitingConditionTimeoutException>();
      }
   }

   public class UpdateWhen_with_Action : IAwaitableShared_specification
   {
      [IAwaitableSharedMatrix] public void executes_action_when_condition_is_immediately_true()
      {
         var shared = _factory.Create(new SharedTestValue { Items = [1] });
         shared.UpdateWhen(v => v.Items.Count > 0, v => v.Items.Add(2));
         shared.Read(v => v.Items.Count).Must().Be(2);
      }
   }

   public class TryUpdateWhen : IAwaitableShared_specification
   {
      [IAwaitableSharedMatrix] public void returns_true_and_executes_update_when_condition_is_immediately_true()
      {
         var shared = _factory.Create(new SharedTestValue());
         shared.TryUpdateWhen(_ => true, v => v.Items.Add(42)).Must().BeTrue();
         shared.Read(v => v.Items.Count).Must().Be(1);
      }

      [IAwaitableSharedMatrix] public void returns_true_when_condition_becomes_true_within_timeout()
      {
         var shared = _factory.Create(new SharedTestValue(), LockTimeout.Seconds(30), WaitTimeout.Seconds(30));
         var tryCompleted = IThreadGate.NewOpen(WaitTimeout.Seconds(10), "tryCompleted");

         _runner.Run(() =>
         {
            shared.TryUpdateWhen(v => v.Items.Count > 0, v => v.Items.Add(99)).Must().BeTrue();
            tryCompleted.AwaitPassThrough();
         });

         tryCompleted.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Milliseconds(100)).Must().BeFalse();
         shared.Update(v => v.Items.Add(1));
         tryCompleted.AwaitPassedThroughCountEqualTo(1);
      }

      [IAwaitableSharedMatrix] public void returns_false_when_condition_never_becomes_true_within_timeout()
      {
         var shared = _factory.Create(new SharedTestValue(), waitTimeout: WaitTimeout.Milliseconds(100));
         shared.TryUpdateWhen(_ => false, _ => {}).Must().BeFalse();
      }
   }

   public class Await : IAwaitableShared_specification
   {
      [IAwaitableSharedMatrix] public void returns_immediately_when_condition_is_already_true()
      {
         var shared = _factory.Create(new SharedTestValue { Value = 42 });
         shared.Await(_ => true);
      }

      [IAwaitableSharedMatrix] public void waits_until_condition_becomes_true()
      {
         var shared = _factory.Create(new SharedTestValue(), LockTimeout.Seconds(30), WaitTimeout.Seconds(30));
         var awaitCompleted = IThreadGate.NewOpen(WaitTimeout.Seconds(10), "awaitCompleted");

         _runner.Run(() =>
         {
            shared.Await(v => v.Items.Count > 0);
            awaitCompleted.AwaitPassThrough();
         });

         awaitCompleted.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Milliseconds(100)).Must().BeFalse();
         shared.Update(v => v.Items.Add(1));
         awaitCompleted.AwaitPassedThroughCountEqualTo(1);
      }

      [IAwaitableSharedMatrix] public void throws_AwaitingConditionTimeoutException_when_condition_never_becomes_true()
      {
         var shared = _factory.Create(new SharedTestValue(), waitTimeout: WaitTimeout.Milliseconds(100));
         Invoking(() => shared.Await(_ => false))
           .Must().Throw<AwaitingConditionTimeoutException>();
      }
   }

   public class CriticalSection_property : IAwaitableShared_specification
   {
      [IAwaitableSharedMatrix] public void exposes_ContentionCount()
      {
         var shared = _factory.Create(new SharedTestValue());

         using(shared.CriticalSection.TakeUpdateLock()) {}

         shared.CriticalSection.ContentionCount.Must().Be(0L);
      }

      [IAwaitableSharedMatrix] public void independently_created_shared_instances_have_different_CriticalSections()
      {
         var sharedA = _factory.Create(new SharedTestValue());
         var sharedB = _factory.Create(new SharedTestValue());

         sharedA.CriticalSection.Must().NotBe(sharedB.CriticalSection);
      }
   }
}
