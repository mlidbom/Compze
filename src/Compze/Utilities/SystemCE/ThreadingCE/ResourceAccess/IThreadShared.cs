using System;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE.ActionFuncHarmonization;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public interface IThreadShared
{
   public static IThreadShared<TShared> WithDefaultTimeout<TShared>() where TShared : new() =>
      new MonitorCEThreadShared<TShared>(new TShared(), MonitorCE.WithDefaultTimeout());

   public static IThreadShared<TShared> WithDefaultTimeout<TShared>(TShared shared) =>
      new MonitorCEThreadShared<TShared>(shared, MonitorCE.WithDefaultTimeout());

   class MonitorCEThreadShared<TShared> : IThreadShared<TShared>
   {
      readonly MonitorCE _monitor;

      readonly TShared _shared;

      internal MonitorCEThreadShared(TShared shared, MonitorCE monitor)
      {
         _shared = shared;
         _monitor = monitor;
      }

      public TResult Read<TResult>(Func<TShared, TResult> read) =>
         _monitor.Read(() => read(_shared));

      public TResult Update<TResult>(Func<TShared, TResult> update) =>
         _monitor.Update(() => update(_shared));

      public unit Update(Action<TShared> update) =>
         _monitor.Update(() => update.AsFunc()(_shared));

      public unit Await(Func<TShared, bool> condition) => _monitor.Await(() => condition(_shared));
      public unit Await(TimeSpan timeout, Func<TShared, bool> condition) => _monitor.Await(timeout, () => condition(_shared));
   }
}

public interface IThreadShared<out TResource>
{
   TResult Read<TResult>(Func<TResource, TResult> read);
   TResult Update<TResult>(Func<TResource, TResult> update);
   unit Update(Action<TResource> update);
   unit Await(Func<TResource, bool> condition);
   unit Await(TimeSpan timeout, Func<TResource, bool> condition);
}
