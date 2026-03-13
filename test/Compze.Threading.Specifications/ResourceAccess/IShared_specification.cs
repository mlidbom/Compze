using Compze.Internals.SystemCE;
using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.Threading.Specifications.TestInfrastructure;
using Compze.Threading.Testing;
using Xunit;
// ReSharper disable InconsistentNaming

namespace Compze.Threading.Specifications.ResourceAccess;

[Collection(nameof(NonParallelCollection))]
public class IShared_specification : UniversalTestBase
{
   readonly ISharedMatrixAttribute.Factory<IShared_specification> _factory = new();
   readonly TestingTaskRunner _runner = new(timeout: 30.Seconds());

   protected override void DisposeInternal()
   {
      _runner.Dispose();
      _factory.Dispose();
   }

   public class Locked_with_Func : IShared_specification
   {
      [ISharedMatrix] public void returns_the_value_from_the_function()
      {
         var shared = _factory.Create(42);
         shared.Locked(value => value).Must().Be(42);
      }

      [ISharedMatrix] public void provides_the_shared_value_to_the_function()
      {
         var shared = _factory.Create("hello");
         shared.Locked(value => value.Length).Must().Be(5);
      }
   }

   public class Locked_with_Action : IShared_specification
   {
      [ISharedMatrix] public void executes_the_action_with_the_shared_value()
      {
         var list = new List<int>();
         var shared = _factory.Create(list);
         shared.Locked(value => value.Add(42));
         list.Must().HaveCount(1);
      }
   }

   public class Locked_provides_mutual_exclusion : IShared_specification
   {
      [ISharedMatrix] public void only_one_thread_can_execute_within_Locked_at_a_time()
      {
         var shared = _factory.Create(new object(), LockTimeout.Seconds(30));
         var insideLockGate = IThreadGate.NewClosed(WaitTimeout.Seconds(30), "insideLock");

         _runner.Run(
            () => shared.Locked(_ => insideLockGate.AwaitPassThrough()),
            () => shared.Locked(_ => insideLockGate.AwaitPassThrough()));

         insideLockGate.AwaitQueueLengthEqualTo(1);
         insideLockGate.TryAwaitQueueLengthEqualTo(2, WaitTimeout.Milliseconds(50)).Must().BeFalse();
         insideLockGate.Open();
         insideLockGate.AwaitPassedThroughCountEqualTo(2);
      }

      [ISharedMatrix] public void second_thread_gains_access_after_first_thread_releases_lock()
      {
         var shared = _factory.Create(new object(), LockTimeout.Seconds(30));
         var firstThreadInsideLock = IThreadGate.NewClosed(WaitTimeout.Seconds(30), "firstThreadInsideLock");
         var secondThreadInsideLock = IThreadGate.NewOpen(WaitTimeout.Seconds(30), "secondThreadInsideLock");

         _runner.Run(() => shared.Locked(_ => firstThreadInsideLock.AwaitPassThrough()));
         firstThreadInsideLock.AwaitQueueLengthEqualTo(1);

         _runner.Run(() => shared.Locked(_ => secondThreadInsideLock.AwaitPassThrough()));
         secondThreadInsideLock.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Milliseconds(50)).Must().BeFalse();

         firstThreadInsideLock.Open();
         secondThreadInsideLock.AwaitPassedThroughCountEqualTo(1);
      }
   }

   public class CriticalSection_property : IShared_specification
   {
      [ISharedMatrix] public void exposes_ContentionCount()
      {
         var shared = _factory.Create(new object());

         using(shared.CriticalSection.TakeLock()) {}

         shared.CriticalSection.ContentionCount.Must().Be(0L);
      }

      [ISharedMatrix] public void independently_created_shared_instances_have_different_CriticalSections()
      {
         var sharedA = _factory.Create(new object());
         var sharedB = _factory.Create(new object());

         sharedA.CriticalSection.Must().NotBe(sharedB.CriticalSection);
      }
   }
}
