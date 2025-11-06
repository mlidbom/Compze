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

      public TResult Read<TResult>(Func<TShared, TResult> read) => _lock.Read(() => read(_shared));
      public unit Read(Action<TShared> read) => Read(read.AsFunc());

      public TResult ReadWhen<TResult>(Func<TShared, TResult> read, Func<bool> condition) => _lock.ReadWhen(() => read(_shared), condition);


      public TResult Update<TResult>(Func<TShared, TResult> update) => _lock.Update(() => update(_shared));
      public unit Update(Action<TShared> update) => Update(update.AsFunc());

      public TResult UpdateWhen<TResult>(Func<TShared, TResult> update, Func<bool> condition) => _lock.UpdateWhen(() => update(_shared), condition);

      public unit Await(Func<TShared, bool> condition) => _lock.Await(() => condition(_shared));
      public unit Await(TimeSpan timeout, Func<TShared, bool> condition) => _lock.Await(() => condition(_shared), timeout);
   }
}

public interface IThreadShared<out TResource>
{
   TResult Read<TResult>(Func<TResource, TResult> read);
   unit Read(Action<TResource> read);
   TResult Update<TResult>(Func<TResource, TResult> update);
   unit Update(Action<TResource> update);
   unit Await(Func<TResource, bool> condition);
   unit Await(TimeSpan timeout, Func<TResource, bool> condition);
}
