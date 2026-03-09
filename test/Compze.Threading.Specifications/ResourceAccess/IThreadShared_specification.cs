using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.Threading.ResourceAccess;
using Compze.Threading.Specifications.TestInfrastructure;
using Xunit;
// ReSharper disable InconsistentNaming

namespace Compze.Threading.Specifications.ResourceAccess;

[Collection(nameof(NonParallelCollection))]
public class IThreadShared_specification : UniversalTestBase
{
   readonly LockFactory<IThreadShared_specification> _lockFactory = new();

   protected override void DisposeInternal() => _lockFactory.Dispose();

   public class Locked_with_Func : IThreadShared_specification
   {
      [PCTLock] public void returns_the_value_from_the_function()
      {
         var shared = IShared.New(42, _lockFactory.CreateLock());
         shared.Locked(value => value).Must().Be(42);
      }

      [PCTLock] public void provides_the_shared_value_to_the_function()
      {
         var shared = IShared.New("hello", _lockFactory.CreateLock());
         shared.Locked(value => value.Length).Must().Be(5);
      }
   }

   public class Locked_with_Action : IThreadShared_specification
   {
      [PCTLock] public void executes_the_action_with_the_shared_value()
      {
         var list = new List<int>();
         var shared = IShared.New(list, _lockFactory.CreateLock());
         shared.Locked(value => value.Add(42));
         list.Must().HaveCount(1);
      }
   }

   public class Lock_property : IThreadShared_specification
   {
      [PCTLock] public void exposes_ContentionCount()
      {
         var @lock = _lockFactory.CreateLock();
         var shared = IShared.New(new object(), @lock);

         using(shared.Lock.TakeLock()) {}

         shared.Lock.ContentionCount.Must().Be(0L);
      }

      [PCTLock] public void shared_instances_with_same_lock_report_same_Lock()
      {
         var @lock = _lockFactory.CreateLock();
         var sharedA = IShared.New(new object(), @lock);
         var sharedB = IShared.New(new object(), @lock);

         sharedA.Lock.Must().Be(sharedB.Lock);
      }
   }
}
