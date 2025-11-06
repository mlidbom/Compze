using System;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE.ActionFuncHarmonization;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public interface IThreadShared
{
   public static IThreadShared<TShared> WithDefaultTimeout<TShared>() where TShared : new() =>
      new MonitorCEThreadShared<TShared>(new TShared(), LockCE.WithDefaultTimeout());

   public static IThreadShared<TShared> WithDefaultTimeout<TShared>(TShared shared) =>
      new MonitorCEThreadShared<TShared>(shared, LockCE.WithDefaultTimeout());

   class MonitorCEThreadShared<TShared> : IThreadShared<TShared>
   {
      readonly ILock _monitor;

      readonly TShared _shared;

      internal MonitorCEThreadShared(TShared shared, ILock monitor)
      {
         _shared = shared;
         _monitor = monitor;
      }

      public TResult Read<TResult>(Func<TShared, TResult> read) => _monitor.Read(() => read(_shared));

      public unit Read(Action<TShared> read) => Read(read.AsFunc());

      public TResult Update<TResult>(Func<TShared, TResult> update) => _monitor.Update(() => update(_shared));

      public unit Update(Action<TShared> update) => Update(update.AsFunc());

      public unit Await(Func<TShared, bool> condition) => _monitor.Await(() => condition(_shared));
      public unit Await(TimeSpan timeout, Func<TShared, bool> condition) => _monitor.Await(timeout, () => condition(_shared));
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
