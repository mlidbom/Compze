using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.Threading.Interprocess.ResourceAccess;
using Compze.Threading.Specifications.TestInfrastructure;
using Compze.xUnitBDD;
using Xunit;

namespace Compze.Threading.Specifications.IAwaitableShared_.IAwaitableProcessShared_;

[Collection(nameof(NonParallelCollection))]
public class IProcessShared_specification : UniversalTestBase
{
   public class Global : IProcessShared_specification
   {
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
