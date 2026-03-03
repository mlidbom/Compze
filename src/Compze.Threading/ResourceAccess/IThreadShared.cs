using System;
using Compze.Underscore;

namespace Compze.Threading.ResourceAccess;

public interface IThreadShared
{
   public static IThreadShared<TShared> New<TShared>(TShared shared, LockTimeout? lockTimeout = null) =>
      new LockCEThreadShared<TShared>(shared, IMonitor.New(lockTimeout));

   // Single implementation for both IThreadShared<T> and IAwaitableThreadShared<T>.
   class LockCEThreadShared<TShared> : IThreadShared<TShared>, IAwaitableThreadShared<TShared>
   {
      readonly IMonitor _monitor;
      readonly IAwaitableMonitor _awaitableMonitor;
      readonly TShared _shared;

      internal LockCEThreadShared(TShared shared, IMonitor monitor)
      {
         _shared = shared;
         _monitor = monitor;
         _awaitableMonitor = (IAwaitableMonitor)monitor;
      }

      internal LockCEThreadShared(TShared shared, IAwaitableMonitor awaitableMonitor)
      {
         _shared = shared;
         _monitor = (IMonitor)awaitableMonitor;
         _awaitableMonitor = awaitableMonitor;
      }

      public TResult Locked<TResult>(Func<TShared, TResult> func, LockTimeout? timeout = null) => _monitor.Locked(() => func(_shared), timeout);

      public TResult Read<TResult>(Func<TShared, TResult> read, LockTimeout? timeout = null) => _awaitableMonitor.Read(() => read(_shared), timeout);

      public TResult ReadWhen<TResult>(Func<TShared, bool> condition, Func<TShared, TResult> read, WaitTimeout? timeout = null) => _awaitableMonitor.ReadWhen(() => condition(_shared), () => read(_shared), timeout);

      public TResult Update<TResult>(Func<TShared, TResult> update, LockTimeout? timeout = null) => _awaitableMonitor.Update(() => update(_shared), timeout);

      public TResult UpdateWhen<TResult>(Func<TShared, bool> condition, Func<TShared, TResult> update, WaitTimeout? timeout = null) => _awaitableMonitor.UpdateWhen(() => condition(_shared), () => update(_shared), timeout);
   }
}

public interface IThreadShared<out TShared>
{
   TResult Locked<TResult>(Func<TShared, TResult> func, LockTimeout? timeout = null);

   unit Locked(Action<TShared> action, LockTimeout? timeout = null) => Locked(action.AsFunc(), timeout);
}
