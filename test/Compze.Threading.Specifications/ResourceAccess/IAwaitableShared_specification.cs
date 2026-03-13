using Compze.Internals.SystemCE;
using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.Threading.Exceptions;
using Compze.Threading.ResourceAccess;
using Compze.Threading.Specifications.TestInfrastructure;
using Compze.Threading.Testing;
using Xunit;
using static Compze.Must.MustActions;
// ReSharper disable InconsistentNaming

namespace Compze.Threading.Specifications.ResourceAccess;

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
         var shared = _factory.Create(42);
         shared.Read(value => value).Must().Be(42);
      }

      [IAwaitableSharedMatrix] public void provides_the_shared_value_to_the_function()
      {
         var shared = _factory.Create("hello");
         shared.Read(value => value.Length).Must().Be(5);
      }
   }

   public class Read_with_Action : IAwaitableShared_specification
   {
      [IAwaitableSharedMatrix] public void executes_the_action_with_the_shared_value()
      {
         var captured = new List<int>();
         var shared = _factory.Create(captured);
         shared.Read(value => value.Count);
         captured.Must().HaveCount(0);
      }
   }

   public class Update_with_Func : IAwaitableShared_specification
   {
      [IAwaitableSharedMatrix] public void returns_the_value_from_the_function()
      {
         var shared = _factory.Create(42);
         shared.Update(value => value + 1).Must().Be(43);
      }

      [IAwaitableSharedMatrix] public void provides_the_shared_value_to_the_function()
      {
         var shared = _factory.Create("hello");
         shared.Update(value => value.Length).Must().Be(5);
      }
   }

   public class Update_with_Action : IAwaitableShared_specification
   {
      [IAwaitableSharedMatrix] public void executes_the_action_with_the_shared_value()
      {
         var list = new List<int>();
         var shared = _factory.Create(list);
         shared.Update(value => value.Add(42));
         list.Must().HaveCount(1);
      }
   }

   static void AssertAllAccessMethodsFail(IAwaitableShared<object> shared)
   {
      Invoking(() => shared.Read(_ => 0, LockTimeout.Milliseconds(10)))
        .Must().Throw<TakeLockTimeoutException>();

      Invoking(() => shared.Read(_ => {}, LockTimeout.Milliseconds(10)))
        .Must().Throw<TakeLockTimeoutException>();

      Invoking(() => shared.Update(_ => 0, LockTimeout.Milliseconds(10)))
        .Must().Throw<TakeLockTimeoutException>();

      Invoking(() => shared.Update(_ => {}, LockTimeout.Milliseconds(10)))
        .Must().Throw<TakeLockTimeoutException>();
   }

   static void AssertAllAccessMethodsSucceed(IAwaitableShared<object> shared)
   {
      shared.Read(_ => 0, LockTimeout.Milliseconds(100));
      shared.Read(_ => {}, LockTimeout.Milliseconds(100));
      shared.Update(_ => 0, LockTimeout.Milliseconds(100));
      shared.Update(_ => {}, LockTimeout.Milliseconds(100));
   }

   static void AssertAllAccessMethodsFailsWhileGateIsClosedAllSucceedAfterOpeningGate(IAwaitableShared<object> shared, IThreadGate insideLock)
   {
      AssertAllAccessMethodsFail(shared);
      insideLock.Open();
      insideLock.AwaitPassedThroughCountEqualTo(1);
      AssertAllAccessMethodsSucceed(shared);
   }

   public class No_other_thread_can_access_the_the_shared_data : IAwaitableShared_specification
   {
      void AssertAccessMethodExcludesAllOtherAccess(Action<IAwaitableShared<object>, IThreadGate> accessMethodUnderTest)
      {
         var shared = _factory.Create(new object(), LockTimeout.Seconds(30));
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
   }

   public class ReadWhen : IAwaitableShared_specification
   {
      [IAwaitableSharedMatrix] public void returns_value_when_condition_is_immediately_true()
      {
         var shared = _factory.Create(42);
         shared.ReadWhen(_ => true, value => value).Must().Be(42);
      }

      [IAwaitableSharedMatrix] public void waits_until_condition_becomes_true_then_returns_value()
      {
         var shared = _factory.Create(new List<int>(), LockTimeout.Seconds(30), WaitTimeout.Seconds(30));
         var readCompleted = IThreadGate.NewOpen(WaitTimeout.Seconds(10), "readCompleted");

         _runner.Run(() =>
         {
            shared.ReadWhen(list => list.Count > 0, list => list[0]).Must().Be(99);
            readCompleted.AwaitPassThrough();
         });

         readCompleted.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Milliseconds(100)).Must().BeFalse();
         shared.Update(list => list.Add(99));
         readCompleted.AwaitPassedThroughCountEqualTo(1);
      }

      [IAwaitableSharedMatrix] public void throws_AwaitingConditionTimeoutException_when_condition_never_becomes_true()
      {
         var shared = _factory.Create(0, waitTimeout: WaitTimeout.Milliseconds(100));
         Invoking(() => shared.ReadWhen(_ => false, value => value))
           .Must().Throw<AwaitingConditionTimeoutException>();
      }
   }

   public class UpdateWhen_with_Func : IAwaitableShared_specification
   {
      [IAwaitableSharedMatrix] public void returns_value_when_condition_is_immediately_true()
      {
         var shared = _factory.Create(42);
         shared.UpdateWhen(_ => true, value => value + 1).Must().Be(43);
      }

      [IAwaitableSharedMatrix] public void waits_until_condition_becomes_true_then_executes_update()
      {
         var shared = _factory.Create(new List<int>(), LockTimeout.Seconds(30), WaitTimeout.Seconds(30));
         var updateCompleted = IThreadGate.NewOpen(WaitTimeout.Seconds(10), "updateCompleted");

         _runner.Run(() =>
         {
            shared.UpdateWhen(list => list.Count > 0, list => list[0]);
            updateCompleted.AwaitPassThrough();
         });

         updateCompleted.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Milliseconds(100)).Must().BeFalse();
         shared.Update(list => list.Add(42));
         updateCompleted.AwaitPassedThroughCountEqualTo(1);
      }

      [IAwaitableSharedMatrix] public void throws_AwaitingConditionTimeoutException_when_condition_never_becomes_true()
      {
         var shared = _factory.Create(0, waitTimeout: WaitTimeout.Milliseconds(100));
         Invoking(() => shared.UpdateWhen(_ => false, value => value))
           .Must().Throw<AwaitingConditionTimeoutException>();
      }
   }

   public class UpdateWhen_with_Action : IAwaitableShared_specification
   {
      [IAwaitableSharedMatrix] public void executes_action_when_condition_is_immediately_true()
      {
         var list = new List<int> { 1 };
         var shared = _factory.Create(list);
         shared.UpdateWhen(l => l.Count > 0, l => l.Add(2));
         list.Must().HaveCount(2);
      }
   }

   public class TryUpdateWhen : IAwaitableShared_specification
   {
      [IAwaitableSharedMatrix] public void returns_true_and_executes_update_when_condition_is_immediately_true()
      {
         var list = new List<int>();
         var shared = _factory.Create(list);
         shared.TryUpdateWhen(_ => true, l => l.Add(42)).Must().BeTrue();
         list.Must().HaveCount(1);
      }

      [IAwaitableSharedMatrix] public void returns_true_when_condition_becomes_true_within_timeout()
      {
         var shared = _factory.Create(new List<int>(), LockTimeout.Seconds(30), WaitTimeout.Seconds(30));
         var tryCompleted = IThreadGate.NewOpen(WaitTimeout.Seconds(10), "tryCompleted");

         _runner.Run(() =>
         {
            shared.TryUpdateWhen(list => list.Count > 0, list => list.Add(99)).Must().BeTrue();
            tryCompleted.AwaitPassThrough();
         });

         tryCompleted.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Milliseconds(100)).Must().BeFalse();
         shared.Update(list => list.Add(1));
         tryCompleted.AwaitPassedThroughCountEqualTo(1);
      }

      [IAwaitableSharedMatrix] public void returns_false_when_condition_never_becomes_true_within_timeout()
      {
         var shared = _factory.Create(0, waitTimeout: WaitTimeout.Milliseconds(100));
         shared.TryUpdateWhen(_ => false, _ => {}).Must().BeFalse();
      }
   }

   public class Await : IAwaitableShared_specification
   {
      [IAwaitableSharedMatrix] public void returns_immediately_when_condition_is_already_true()
      {
         var shared = _factory.Create(42);
         shared.Await(_ => true);
      }

      [IAwaitableSharedMatrix] public void waits_until_condition_becomes_true()
      {
         var shared = _factory.Create(new List<int>(), LockTimeout.Seconds(30), WaitTimeout.Seconds(30));
         var awaitCompleted = IThreadGate.NewOpen(WaitTimeout.Seconds(10), "awaitCompleted");

         _runner.Run(() =>
         {
            shared.Await(list => list.Count > 0);
            awaitCompleted.AwaitPassThrough();
         });

         awaitCompleted.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Milliseconds(100)).Must().BeFalse();
         shared.Update(list => list.Add(1));
         awaitCompleted.AwaitPassedThroughCountEqualTo(1);
      }

      [IAwaitableSharedMatrix] public void throws_AwaitingConditionTimeoutException_when_condition_never_becomes_true()
      {
         var shared = _factory.Create(0, waitTimeout: WaitTimeout.Milliseconds(100));
         Invoking(() => shared.Await(_ => false))
           .Must().Throw<AwaitingConditionTimeoutException>();
      }
   }

   public class CriticalSection_property : IAwaitableShared_specification
   {
      [IAwaitableSharedMatrix] public void exposes_ContentionCount()
      {
         var shared = _factory.Create(new object());

         using(shared.CriticalSection.TakeUpdateLock()) {}

         shared.CriticalSection.ContentionCount.Must().Be(0L);
      }

      [IAwaitableSharedMatrix] public void independently_created_shared_instances_have_different_CriticalSections()
      {
         var sharedA = _factory.Create(new object());
         var sharedB = _factory.Create(new object());

         sharedA.CriticalSection.Must().NotBe(sharedB.CriticalSection);
      }
   }
}
