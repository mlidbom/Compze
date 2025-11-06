using System;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE.ActionFuncHarmonization;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public interface IThreadShared
{
   public static IThreadShared<TShared> WithDefaultTimeout<TShared>() where TShared : new() =>
      new LockCEThreadShared<TShared>(new TShared(), ILock.WithDefaultTimeout());

   public static IThreadShared<TShared> WithDefaultTimeout<TShared>(TShared shared) =>
      new LockCEThreadShared<TShared>(shared, ILock.WithDefaultTimeout());

   public static IThreadShared<TShared> WithTimeout<TShared>(TimeSpan timeout) where TShared : new() =>
      new LockCEThreadShared<TShared>(new TShared(), ILock.WithTimeout(timeout));

   public static IThreadShared<TShared> WithTimeout<TShared>(TShared shared, TimeSpan timeout) =>
      new LockCEThreadShared<TShared>(shared, ILock.WithTimeout(timeout));

   class LockCEThreadShared<TShared> : IThreadShared<TShared>
   {
      readonly ILock _lock;

      readonly TShared _shared;

      internal LockCEThreadShared(TShared shared, ILock @lock)
      {
         _shared = shared;
         _lock = @lock;
      }

      public TResult Read<TResult>(Func<TShared, TResult> read, TimeSpan? timeout = null) => _lock.Read(() => read(_shared), timeout);

      public TReturn ReadOut<TReturn, TOut>(OutReadDelegate<TShared, TReturn, TOut> readOut, out TOut result, TimeSpan? timeout = null)
      {
         using(_lock.TakeReadLock(timeout))
         {
            return readOut(_shared, out result);
         }
      }

      public TReturn ReadOutWhen<TReturn, TOut>(OutReadDelegate<TShared, TReturn, TOut> readOut, Func<TShared, bool> condition, out TOut result, TimeSpan? timeout = null)
      {
         using(_lock.TakeReadLockWhen(() => condition(_shared), timeout))
         {
            return readOut(_shared, out result);
         }
      }

      public TResult ReadWhen<TResult>(Func<TShared, TResult> read, Func<TShared, bool> condition, TimeSpan? timeout = null) => _lock.ReadWhen(() => read(_shared), () => condition(_shared), timeout);

      public TResult Update<TResult>(Func<TShared, TResult> update, TimeSpan? timeout = null) => _lock.Update(() => update(_shared), timeout);

      public TResult UpdateWhen<TResult>(Func<TShared, TResult> update, Func<TShared, bool> condition, TimeSpan? timeout = null) => _lock.UpdateWhen(() => update(_shared), () => condition(_shared), timeout);
   }
}

public delegate TReturn OutReadDelegate<in TShared, out TReturn, TOut>(TShared shared, out TOut result);

public interface IThreadShared<out TShared>
{
   //core
   TReturn ReadOut<TReturn, TOut>(OutReadDelegate<TShared, TReturn, TOut> readOut, out TOut result, TimeSpan? timeout = null);
   TReturn ReadOutWhen<TReturn, TOut>(OutReadDelegate<TShared, TReturn, TOut> readOut, Func<TShared, bool> condition, out TOut result, TimeSpan? timeout = null);

   TResult Read<TResult>(Func<TShared, TResult> read, TimeSpan? timeout = null);
   TResult ReadWhen<TResult>(Func<TShared, TResult> read, Func<TShared, bool> condition, TimeSpan? timeout = null);
   TResult Update<TResult>(Func<TShared, TResult> update, TimeSpan? timeout = null);
   TResult UpdateWhen<TResult>(Func<TShared, TResult> update, Func<TShared, bool> condition, TimeSpan? timeout = null);

   //Default implementations
   unit Read(Action<TShared> read, TimeSpan? timeout = null) => Read(read.AsFunc(), timeout);
   unit ReadWhen(Action<TShared> read, Func<TShared, bool> condition, TimeSpan? timeout = null) => ReadWhen(read.AsFunc(), condition, timeout);
   unit Update(Action<TShared> update, TimeSpan? timeout = null) => Update(update.AsFunc(), timeout);
   unit UpdateWhen(Action<TShared> update, Func<TShared, bool> condition, TimeSpan? timeout = null) => UpdateWhen(update.AsFunc(), condition, timeout);

   unit Await(Func<TShared, bool> condition, TimeSpan? timeout = null) => ReadWhen(it => {}, condition, timeout);
}
