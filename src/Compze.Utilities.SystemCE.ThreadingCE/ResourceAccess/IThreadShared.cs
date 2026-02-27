using System;
using Compze.Functional;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public interface IThreadShared
{
   public static IThreadShared<TShared> WithDefaultTimeouts<TShared>() where TShared : new() =>
      new LockCEThreadShared<TShared>(new TShared(), IMonitor.WithDefaultTimeout());

   public static IThreadShared<TShared> WithDefaultTimeouts<TShared>(TShared shared) =>
      new LockCEThreadShared<TShared>(shared, IMonitor.WithDefaultTimeout());

   public static IThreadShared<TShared> New<TShared>(LockTimeout lockTimeout) where TShared : new() =>
      new LockCEThreadShared<TShared>(new TShared(), IMonitor.New(lockTimeout));

   public static IThreadShared<TShared> New<TShared>(TShared shared, LockTimeout lockTimeout) =>
      new LockCEThreadShared<TShared>(shared, IMonitor.New(lockTimeout));

   // Single implementation for both IThreadShared<T> and IAwaitableThreadShared<T>.
   public class LockCEThreadShared<TShared> : IThreadShared<TShared>, IAwaitableThreadShared<TShared>
   {
      readonly IMonitor _monitor;
      readonly IAwaitableMonitor _awaitableMonitor;
      readonly TShared _shared;

      public LockCEThreadShared(TShared shared, IMonitor monitor)
      {
         _shared = shared;
         _monitor = monitor;
         _awaitableMonitor = (IAwaitableMonitor)monitor;
      }

      public LockCEThreadShared(TShared shared, IAwaitableMonitor awaitableMonitor)
      {
         _shared = shared;
         _monitor = (IMonitor)awaitableMonitor;
         _awaitableMonitor = awaitableMonitor;
      }

      public TResult Locked<TResult>(Func<TShared, TResult> func, LockTimeout? timeout = null) => _monitor.Locked(() => func(_shared), timeout);

      public TReturn LockedOut<TReturn, TOut>(OutReadFunc<TShared, TReturn, TOut> func, out TOut result, LockTimeout? timeout = null)
      {
         using(_monitor.TakeLock(timeout))
         {
            return func(_shared, out result);
         }
      }

      public TResult Read<TResult>(Func<TShared, TResult> read, LockTimeout? timeout = null) => _awaitableMonitor.Read(() => read(_shared), timeout);

      public TReturn ReadOut<TReturn, TOut>(OutReadFunc<TShared, TReturn, TOut> readOut, out TOut result, LockTimeout? timeout = null)
      {
         using(_awaitableMonitor.TakeReadLock(timeout))
         {
            return readOut(_shared, out result);
         }
      }

      public TReturn ReadOutWhen<TReturn, TOut>(OutReadFunc<TShared, TReturn, TOut> readOut, Func<TShared, bool> condition, out TOut result, WaitTimeout? timeout = null)
      {
         using(_awaitableMonitor.TakeReadLockWhen(() => condition(_shared), timeout))
         {
            return readOut(_shared, out result);
         }
      }

      public TResult ReadOrUpdate<TResult>(Func<TShared, TResult?> tryRead, Action<TShared> updateOnFailedRead, LockTimeout? timeout = null)
         where TResult : class =>
         _awaitableMonitor.ReadOrUpdate(() => tryRead(_shared), () => updateOnFailedRead(_shared));

      public TResult ReadWhen<TResult>(Func<TShared, TResult> read, Func<TShared, bool> condition, WaitTimeout? timeout = null) => _awaitableMonitor.ReadWhen(() => read(_shared), () => condition(_shared), timeout);

      public TResult Update<TResult>(Func<TShared, TResult> update, LockTimeout? timeout = null) => _awaitableMonitor.Update(() => update(_shared), timeout);

      public TResult UpdateWhen<TResult>(Func<TShared, TResult> update, Func<TShared, bool> condition, WaitTimeout? timeout = null) => _awaitableMonitor.UpdateWhen(() => update(_shared), () => condition(_shared), timeout);
   }
}

public delegate TReturn OutReadFunc<in TShared, out TReturn, TOut>(TShared shared, out TOut result);

public interface IThreadShared<out TShared>
{
   TResult Locked<TResult>(Func<TShared, TResult> func, LockTimeout? timeout = null);
   TReturn LockedOut<TReturn, TOut>(OutReadFunc<TShared, TReturn, TOut> func, out TOut result, LockTimeout? timeout = null);

   unit Locked(Action<TShared> action, LockTimeout? timeout = null) => Locked(action.AsFunc(), timeout);
}
