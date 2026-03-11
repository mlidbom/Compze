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

      public IDisposable Enter()
      {
         EntranceGate.AwaitPassThrough();
         return new Disposable(() => ExitGate.AwaitPassThrough());
      }

      public TReturn ExecuteWithExclusiveLock<TReturn>(Func<IGatedCodeSection, TReturn> action) => _sharedLock.Update(() => action(this));
   }
}
