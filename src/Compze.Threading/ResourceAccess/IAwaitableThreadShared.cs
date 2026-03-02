using System;
using Compze.Underscore;

namespace Compze.Threading.ResourceAccess;

public interface IAwaitableThreadShared
{
   public static IAwaitableThreadShared<TShared> WithDefaultTimeouts<TShared>(TShared shared) =>
      new IThreadShared.LockCEThreadShared<TShared>(shared, IAwaitableMonitor.WithDefaultTimeout());

   public static IAwaitableThreadShared<TShared> New<TShared>(TShared shared, LockTimeout lockTimeout, WaitTimeout? waitTimeout = null) =>
      new IThreadShared.LockCEThreadShared<TShared>(shared, IAwaitableMonitor.New(lockTimeout, waitTimeout));
}

public interface IAwaitableThreadShared<out TShared>
{
   //core
   TReturn ReadOut<TReturn, TOut>(OutReadFunc<TShared, TReturn, TOut> readOut, out TOut result, LockTimeout? timeout = null);
   TReturn ReadOutWhen<TReturn, TOut>(OutReadFunc<TShared, TReturn, TOut> readOut, Func<TShared, bool> condition, out TOut result, WaitTimeout? timeout = null);

   TResult Read<TResult>(Func<TShared, TResult> read, LockTimeout? timeout = null);
   TResult ReadWhen<TResult>(Func<TShared, TResult> read, Func<TShared, bool> condition, WaitTimeout? timeout = null);
   TResult Update<TResult>(Func<TShared, TResult> update, LockTimeout? timeout = null);
   TResult UpdateWhen<TResult>(Func<TShared, TResult> update, Func<TShared, bool> condition, WaitTimeout? timeout = null);

   TResult ReadOrUpdate<TResult>(Func<TShared, TResult?> tryRead, Action<TShared> updateOnFailedRead, LockTimeout? timeout = null)
      where TResult : class;

   unit Read(Action<TShared> read, LockTimeout? timeout = null) => Read(read.AsFunc(), timeout);
   unit ReadWhen(Action<TShared> read, Func<TShared, bool> condition, WaitTimeout? timeout = null) => ReadWhen(read.AsFunc(), condition, timeout);
   unit Update(Action<TShared> update, LockTimeout? timeout = null) => Update(update.AsFunc(), timeout);
   unit UpdateWhen(Action<TShared> update, Func<TShared, bool> condition, WaitTimeout? timeout = null) => UpdateWhen(update.AsFunc(), condition, timeout);

   unit Await(Func<TShared, bool> condition, WaitTimeout? timeout = null) => ReadWhen(_ => {}, condition, timeout);
}
