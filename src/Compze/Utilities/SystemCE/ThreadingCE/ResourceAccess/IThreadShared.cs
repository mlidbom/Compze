using System;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE.ActionFuncHarmonization;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public interface IThreadShared
{
   public static IThreadShared<TShared> WithDefaultTimeout<TShared>() where TShared : new() =>
      new MonitorCEThreadShared<TShared>(new TShared(), ILock.WithDefaultTimeout());

   public static IThreadShared<TShared> WithDefaultTimeout<TShared>(TShared shared) =>
      new MonitorCEThreadShared<TShared>(shared, ILock.WithDefaultTimeout());

   public static IThreadShared<TShared> WithTimeout<TShared>(TimeSpan timeout) where TShared : new() =>
      new MonitorCEThreadShared<TShared>(new TShared(), ILock.WithTimeout(timeout));

   public static IThreadShared<TShared> WithTimeout<TShared>(TShared shared, TimeSpan timeout) =>
      new MonitorCEThreadShared<TShared>(shared, ILock.WithTimeout(timeout));

   class MonitorCEThreadShared<TShared> : IThreadShared<TShared>
   {
      readonly ILock _lock;

      readonly TShared _shared;

      internal MonitorCEThreadShared(TShared shared, ILock @lock)
      {
         _shared = shared;
         _lock = @lock;
      }

      public TResult Read<TResult>(Func<TShared, TResult> read, TimeSpan? timeout = null) => _lock.Read(() => read(_shared), timeout);
      public unit Read(Action<TShared> read, TimeSpan? timeout = null) => Read(read.AsFunc(), timeout);

      public TResult ReadWhen<TResult>(Func<TShared, TResult> read, Func<bool> condition, TimeSpan? timeout = null) => _lock.ReadWhen(() => read(_shared), condition, timeout);


      public TResult Update<TResult>(Func<TShared, TResult> update, TimeSpan? timeout = null) => _lock.Update(() => update(_shared), timeout);
      public unit Update(Action<TShared> update, TimeSpan? timeout = null) => Update(update.AsFunc(), timeout);

      public TResult UpdateWhen<TResult>(Func<TShared, TResult> update, Func<bool> condition, TimeSpan? timeout = null) => _lock.UpdateWhen(() => update(_shared), condition, timeout);

      public unit Await(Func<TShared, bool> condition, TimeSpan? timeout = null) => _lock.Await(() => condition(_shared), timeout);
   }
}

public interface IThreadShared<out TResource>
{
   TResult Read<TResult>(Func<TResource, TResult> read, TimeSpan? timeout = null);
   unit Read(Action<TResource> read, TimeSpan? timeout = null);
   TResult Update<TResult>(Func<TResource, TResult> update, TimeSpan? timeout = null);
   unit Update(Action<TResource> update, TimeSpan? timeout = null);
   unit Await(Func<TResource, bool> condition, TimeSpan? timeout = null);
}
