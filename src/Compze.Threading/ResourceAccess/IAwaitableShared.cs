// ReSharper disable ConvertToPrimaryConstructor

namespace Compze.Threading.ResourceAccess;

public interface IAwaitableShared
{
   public static IAwaitableShared<TShared> New<TShared>(TShared shared, IAwaitableLock @lock) =>
      new AwaitableShared<TShared>(shared, @lock);

   internal class AwaitableShared<TShared> : IAwaitableShared<TShared>
   {
      readonly TShared _shared;

      public AwaitableShared(TShared shared, IAwaitableLock @lock)
      {
         _shared = shared;
         Lock = @lock;
      }

      public IAwaitableLock Lock { get; }

      public TResult Read<TResult>(Func<TShared, TResult> read, LockTimeout? timeout = null) =>
         Lock.Read(() => read(_shared), timeout);

      public TResult ReadWhen<TResult>(Func<TShared, bool> condition, Func<TShared, TResult> read, WaitTimeout? timeout = null) =>
         Lock.ReadWhen(() => condition(_shared), () => read(_shared), timeout);

      public TResult Update<TResult>(Func<TShared, TResult> update, LockTimeout? timeout = null) =>
         Lock.Update(() => update(_shared), timeout);

      public TResult UpdateWhen<TResult>(Func<TShared, bool> condition, Func<TShared, TResult> update, WaitTimeout? timeout = null) =>
         Lock.UpdateWhen(() => condition(_shared), () => update(_shared), timeout);

      public bool TryUpdateWhen(Func<TShared, bool> condition, Action<TShared> update, WaitTimeout? timeout = null) =>
         Lock.TryUpdateWhen(() => condition(_shared), () => update(_shared), timeout);
   }
}

public interface IAwaitableShared<out TShared>
{
   IAwaitableLock Lock { get; }

   //core
   TResult Read<TResult>(Func<TShared, TResult> read, LockTimeout? timeout = null);
   TResult ReadWhen<TResult>(Func<TShared, bool> condition, Func<TShared, TResult> read, WaitTimeout? timeout = null);
   TResult Update<TResult>(Func<TShared, TResult> update, LockTimeout? timeout = null);
   TResult UpdateWhen<TResult>(Func<TShared, bool> condition, Func<TShared, TResult> update, WaitTimeout? timeout = null);

   //Default implementations
   unit Read(Action<TShared> read, LockTimeout? timeout = null) => Read(read.ToFunc(), timeout);

   unit Update(Action<TShared> update, LockTimeout? timeout = null) => Update(update.ToFunc(), timeout);
   unit UpdateWhen(Func<TShared, bool> condition, Action<TShared> update, WaitTimeout? timeout = null) => UpdateWhen(condition, update.ToFunc(), timeout);

   unit Await(Func<TShared, bool> condition, WaitTimeout? timeout = null) => ReadWhen(condition, _ => unit.Value, timeout);

   bool TryUpdateWhen(Func<TShared, bool> condition, Action<TShared> update, WaitTimeout? timeout = null);
}
