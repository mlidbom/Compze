using System;
using Compze.Functional;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public interface IThreadShared
{
   public static IThreadShared<TShared> WithDefaultTimeouts<TShared>() where TShared : new() =>
      new LockCEThreadShared<TShared>(new TShared(), IMonitorCE.WithDefaultTimeout());

   public static IThreadShared<TShared> WithDefaultTimeouts<TShared>(TShared shared) =>
      new LockCEThreadShared<TShared>(shared, IMonitorCE.WithDefaultTimeout());

   public static IThreadShared<TShared> WithTimeout<TShared>(TimeSpan lockTimeout) where TShared : new() =>
      new LockCEThreadShared<TShared>(new TShared(), IMonitorCE.WithTimeout(lockTimeout));

   public static IThreadShared<TShared> WithTimeout<TShared>(TShared shared, TimeSpan lockTimeout) =>
      new LockCEThreadShared<TShared>(shared, IMonitorCE.WithTimeout(lockTimeout));

   // Single implementation for both IThreadShared<T> and IAwaitableThreadShared<T>.
   public class LockCEThreadShared<TShared> : IThreadShared<TShared>, IAwaitableThreadShared<TShared>
   {
      readonly IMonitorCE _monitor;
      readonly IAwaitableMonitorCE _awaitableMonitor;
      readonly TShared _shared;

      public LockCEThreadShared(TShared shared, IMonitorCE monitor)
      {
         _shared = shared;
         _monitor = monitor;
         _awaitableMonitor = (IAwaitableMonitorCE)monitor;
      }

      public LockCEThreadShared(TShared shared, IAwaitableMonitorCE awaitableMonitor)
      {
         _shared = shared;
         _monitor = (IMonitorCE)awaitableMonitor;
         _awaitableMonitor = awaitableMonitor;
      }

      public TResult Locked<TResult>(Func<TShared, TResult> func, TimeSpan? timeout = null) => _monitor.Locked(() => func(_shared), timeout);

      public TReturn LockedOut<TReturn, TOut>(OutReadFunc<TShared, TReturn, TOut> func, out TOut result, TimeSpan? timeout = null)
      {
         using(_monitor.TakeLock(timeout))
         {
            return func(_shared, out result);
         }
      }

      public TResult Read<TResult>(Func<TShared, TResult> read, TimeSpan? timeout = null) => _awaitableMonitor.Read(() => read(_shared), timeout);

      public TReturn ReadOut<TReturn, TOut>(OutReadFunc<TShared, TReturn, TOut> readOut, out TOut result, TimeSpan? timeout = null)
      {
         using(_awaitableMonitor.TakeReadLock(timeout))
         {
            return readOut(_shared, out result);
         }
      }

      public TReturn ReadOutWhen<TReturn, TOut>(OutReadFunc<TShared, TReturn, TOut> readOut, Func<TShared, bool> condition, out TOut result, TimeSpan? timeout = null)
      {
         using(_awaitableMonitor.TakeReadLockWhen(() => condition(_shared), timeout))
         {
            return readOut(_shared, out result);
         }
      }

      public TResult ReadOrUpdate<TResult>(Func<TShared, TResult?> tryRead, Action<TShared> updateOnFailedRead, TimeSpan? timeout = null)
         where TResult : class =>
         _awaitableMonitor.ReadOrUpdate(() => tryRead(_shared), () => updateOnFailedRead(_shared));

      public TResult ReadWhen<TResult>(Func<TShared, TResult> read, Func<TShared, bool> condition, TimeSpan? timeout = null) => _awaitableMonitor.ReadWhen(() => read(_shared), () => condition(_shared), timeout);

      public TResult Update<TResult>(Func<TShared, TResult> update, TimeSpan? timeout = null) => _awaitableMonitor.Update(() => update(_shared), timeout);

      public TResult UpdateWhen<TResult>(Func<TShared, TResult> update, Func<TShared, bool> condition, TimeSpan? timeout = null) => _awaitableMonitor.UpdateWhen(() => update(_shared), () => condition(_shared), timeout);
   }
}

public delegate TReturn OutReadFunc<in TShared, out TReturn, TOut>(TShared shared, out TOut result);

public interface IThreadShared<out TShared>
{
   TResult Locked<TResult>(Func<TShared, TResult> func, TimeSpan? timeout = null);
   TReturn LockedOut<TReturn, TOut>(OutReadFunc<TShared, TReturn, TOut> func, out TOut result, TimeSpan? timeout = null);

   unit Locked(Action<TShared> action, TimeSpan? timeout = null) => Locked(action.AsFunc(), timeout);
}
