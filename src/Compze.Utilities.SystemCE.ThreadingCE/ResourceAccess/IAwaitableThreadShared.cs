using System;
using Compze.Functional;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public interface IAwaitableThreadShared
{
   public static IAwaitableThreadShared<TShared> WithDefaultTimeouts<TShared>() where TShared : new() =>
      new IThreadShared.LockCEThreadShared<TShared>(new TShared(), IAwaitableMonitor.WithDefaultTimeout());

   public static IAwaitableThreadShared<TShared> WithDefaultTimeouts<TShared>(TShared shared) =>
      new IThreadShared.LockCEThreadShared<TShared>(shared, IAwaitableMonitor.WithDefaultTimeout());

   public static IAwaitableThreadShared<TShared> WithTimeouts<TShared>(TimeSpan lockTimeout, TimeSpan? waitTimeout = null) where TShared : new() =>
      new IThreadShared.LockCEThreadShared<TShared>(new TShared(), IAwaitableMonitor.WithTimeouts(lockTimeout, waitTimeout));

   public static IAwaitableThreadShared<TShared> WithTimeouts<TShared>(TShared shared, TimeSpan lockTimeout, TimeSpan? waitTimeout = null) =>
      new IThreadShared.LockCEThreadShared<TShared>(shared, IAwaitableMonitor.WithTimeouts(lockTimeout, waitTimeout));
}

public interface IAwaitableThreadShared<out TShared>
{
   //core
   TReturn ReadOut<TReturn, TOut>(OutReadFunc<TShared, TReturn, TOut> readOut, out TOut result, TimeSpan? timeout = null);
   TReturn ReadOutWhen<TReturn, TOut>(OutReadFunc<TShared, TReturn, TOut> readOut, Func<TShared, bool> condition, out TOut result, TimeSpan? timeout = null);

   TResult Read<TResult>(Func<TShared, TResult> read, TimeSpan? timeout = null);
   TResult ReadWhen<TResult>(Func<TShared, TResult> read, Func<TShared, bool> condition, TimeSpan? timeout = null);
   TResult Update<TResult>(Func<TShared, TResult> update, TimeSpan? timeout = null);
   TResult UpdateWhen<TResult>(Func<TShared, TResult> update, Func<TShared, bool> condition, TimeSpan? timeout = null);

   TResult ReadOrUpdate<TResult>(Func<TShared, TResult?> tryRead, Action<TShared> updateOnFailedRead, TimeSpan? timeout = null)
      where TResult : class;

   unit Read(Action<TShared> read, TimeSpan? timeout = null) => Read(read.AsFunc(), timeout);
   unit ReadWhen(Action<TShared> read, Func<TShared, bool> condition, TimeSpan? timeout = null) => ReadWhen(read.AsFunc(), condition, timeout);
   unit Update(Action<TShared> update, TimeSpan? timeout = null) => Update(update.AsFunc(), timeout);
   unit UpdateWhen(Action<TShared> update, Func<TShared, bool> condition, TimeSpan? timeout = null) => UpdateWhen(update.AsFunc(), condition, timeout);

   unit Await(Func<TShared, bool> condition, TimeSpan? timeout = null) => ReadWhen(it => {}, condition, timeout);
}
