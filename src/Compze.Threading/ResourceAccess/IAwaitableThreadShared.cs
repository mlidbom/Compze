using System;
using Compze.Underscore;

namespace Compze.Threading.ResourceAccess;

public interface IAwaitableThreadShared
{
   public static IAwaitableThreadShared<TShared> New<TShared>(TShared shared, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null) =>
      IAwaitableThreadShared<TShared>.New(shared, lockTimeout, waitTimeout);
}

public interface IAwaitableThreadShared<out TShared>
{
   public static IAwaitableThreadShared<TShared> New(TShared shared, LockTimeout? lockTimeout = null, WaitTimeout? waitTimeout = null) =>
      new IThreadShared.LockCEThreadShared<TShared>(shared, IAwaitableMonitor.New(lockTimeout, waitTimeout));

   //core
   TResult Read<TResult>(Func<TShared, TResult> read, LockTimeout? timeout = null);
   TResult ReadWhen<TResult>(Func<TShared, bool> condition, Func<TShared, TResult> read, WaitTimeout? timeout = null);
   TResult Update<TResult>(Func<TShared, TResult> update, LockTimeout? timeout = null);
   TResult UpdateWhen<TResult>(Func<TShared, bool> condition, Func<TShared, TResult> update, WaitTimeout? timeout = null);

   //Default implementations
   unit Update(Action<TShared> update, LockTimeout? timeout = null) => Update(update.AsFunc(), timeout);
   unit UpdateWhen(Func<TShared, bool> condition, Action<TShared> update, WaitTimeout? timeout = null) => UpdateWhen(condition, update.AsFunc(), timeout);

   unit Await(Func<TShared, bool> condition, WaitTimeout? timeout = null) => ReadWhen(condition, _ => unit.Value, timeout);
}
