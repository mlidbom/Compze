using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Threading.Testing;

public partial interface IGatedCodeSection
{
   private class Implementation : IGatedCodeSection
   {
      readonly IAwaitableMonitor _sharedLock;

      public IThreadGate EntranceGate { get; }
      public IThreadGate ExitGate { get; }

      internal Implementation(WaitTimeout timeout, string name)
      {
         _sharedLock = IAwaitableMonitor.New(LockTimeout.Default, timeout);
         EntranceGate = IThreadGate.NewClosed(timeout, _sharedLock, $"{name}.Entrance");
         ExitGate = IThreadGate.NewClosed(timeout, _sharedLock, $"{name}.Exit");
      }

      public TReturn Execute<TReturn>(Func<TReturn> func)
      {
         EntranceGate.AwaitPassThrough();
         try
         {
            return func();
         }
         finally
         {
            ExitGate.AwaitPassThrough();
         }
      }

      public async Task<TReturn> ExecuteAsync<TReturn>(Func<Task<TReturn>> func)
      {
         await EntranceGate.AwaitPassThroughAsync().caf();
         try
         {
            return await func().caf();
         }
         finally
         {
            await ExitGate.AwaitPassThroughAsync().caf();
         }
      }

      public TReturn ExecuteWithExclusiveLock<TReturn>(Func<IGatedCodeSection, TReturn> action) => _sharedLock.Update(() => action(this));
   }
}
