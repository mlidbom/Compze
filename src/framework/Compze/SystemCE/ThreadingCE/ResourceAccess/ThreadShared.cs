﻿using System;
using Compze.SystemCE.ReflectionCE;

namespace Compze.SystemCE.ThreadingCE.ResourceAccess;

interface IThreadShared<out TResource>
{
   TResult Read<TResult>(Func<TResource, TResult> read);
   TResult Update<TResult>(Func<TResource, TResult> update);
   void Update(Action<TResource> update);
   void Await(Func<TResource, bool> condition);
   void Await(TimeSpan timeout, Func<TResource, bool> condition);
}

static class ThreadShared
{
   public static IThreadShared<TShared> WithDefaultTimeout<TShared>() where TShared : new() =>
      new MonitorCEThreadShared<TShared>(Constructor.For<TShared>.DefaultConstructor.Instance(), MonitorCE.WithDefaultTimeout());

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

      public void Update(Action<TShared> update) =>
         _monitor.Update(() => update(_shared));

      public void Await(Func<TShared, bool> condition) => _monitor.Await(() => condition(_shared));
      public void Await(TimeSpan timeout, Func<TShared, bool> condition) => _monitor.Await(timeout, () => condition(_shared));
   }
}