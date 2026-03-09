using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.Threading.Interprocess;
using Compze.Threading.Specifications.TestInfrastructure;
using Compze.xUnitBDD;
using Xunit;

namespace Compze.Threading.Specifications.Interprocess;

[Collection(nameof(NonParallelCollection))]
public class IProcessShared_specification : UniversalTestBase
{
   public class Global : IProcessShared_specification
   {
      [XF] public void returns_the_value_from_the_locked_function()
      {
         var shared = IProcessShared.Global("IProcessShared_specification.Global.returns_value", 42, timeout: null, onAbandonedMutexException: null);
         shared.Locked(value => value).Must().Be(42);
      }

      [XF] public void provides_the_shared_value_to_the_function()
      {
         var shared = IProcessShared.Global("IProcessShared_specification.Global.provides_shared", "hello", timeout: null, onAbandonedMutexException: null);
         shared.Locked(value => value).Must().Be("hello");
      }

      [XF] public void executes_the_action_with_the_shared_value()
      {
         var list = new List<int>();
         var shared = IProcessShared.Global("IProcessShared_specification.Global.executes_action", list, timeout: null, onAbandonedMutexException: null);
         shared.Locked(value => value.Add(42));
         list.Must().HaveCount(1);
      }

      [XF] public void Mutex_IsGlobal_is_true()
      {
         var shared = IProcessShared.Global("IProcessShared_specification.Global.Mutex_IsGlobal", 0, timeout: null, onAbandonedMutexException: null);
         shared.Mutex.IsGlobal.Must().BeTrue();
      }

      [XF] public void Mutex_Name_is_prefixed_with_Global()
      {
         var shared = IProcessShared.Global("IProcessShared_specification.Global.Mutex_Name", 0, timeout: null, onAbandonedMutexException: null);
         shared.Mutex.Name.Must().Be(@"Global\IProcessShared_specification.Global.Mutex_Name");
      }

      [XF] public void Mutex_LockTimeout_matches_specified_timeout()
      {
         var shared = IProcessShared.Global("IProcessShared_specification.Global.Mutex_LockTimeout", 0, timeout: LockTimeout.Seconds(7), onAbandonedMutexException: null);
         shared.Mutex.LockTimeout.Must().Be(LockTimeout.Seconds(7));
      }
   }

   public class Local : IProcessShared_specification
   {
      [XF] public void returns_the_value_from_the_locked_function()
      {
         var shared = IProcessShared.Local("IProcessShared_specification.Local.returns_value", 42, timeout: null, onAbandonedMutexException: null);
         shared.Locked(value => value).Must().Be(42);
      }

      [XF] public void provides_the_shared_value_to_the_function()
      {
         var shared = IProcessShared.Local("IProcessShared_specification.Local.provides_shared", "hello", timeout: null, onAbandonedMutexException: null);
         shared.Locked(value => value.Length).Must().Be(5);
      }

      [XF] public void executes_the_action_with_the_shared_value()
      {
         var list = new List<int>();
         var shared = IProcessShared.Local("IProcessShared_specification.Local.executes_action", list, timeout: null, onAbandonedMutexException: null);
         shared.Locked(value => value.Add(42));
         list.Must().HaveCount(1);
      }

      [XF] public void Mutex_IsGlobal_is_false()
      {
         var shared = IProcessShared.Local("IProcessShared_specification.Local.Mutex_IsGlobal", 0, timeout: null, onAbandonedMutexException: null);
         shared.Mutex.IsGlobal.Must().BeFalse();
      }

      [XF] public void Mutex_Name_is_prefixed_with_Local()
      {
         var shared = IProcessShared.Local("IProcessShared_specification.Local.Mutex_Name", 0, timeout: null, onAbandonedMutexException: null);
         shared.Mutex.Name.Must().Be(@"Local\IProcessShared_specification.Local.Mutex_Name");
      }

      [XF] public void Mutex_LockTimeout_matches_specified_timeout()
      {
         var shared = IProcessShared.Local("IProcessShared_specification.Local.Mutex_LockTimeout", 0, timeout: LockTimeout.Seconds(7), onAbandonedMutexException: null);
         shared.Mutex.LockTimeout.Must().Be(LockTimeout.Seconds(7));
      }
   }
}
