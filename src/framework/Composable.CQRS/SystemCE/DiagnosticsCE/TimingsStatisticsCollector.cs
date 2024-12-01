﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Composable.SystemCE.ThreadingCE.ResourceAccess;
using Composable.SystemCE.ThreadingCE.TasksCE;

// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable UnusedMember.Local

namespace Composable.SystemCE.DiagnosticsCE;

//Todo: Replace implementation with App.Metrics https://www.app-metrics.io/getting-started/metric-types/timers/. Design a wrapper around App.Metrics that can be injected and used as simply and flexibly as this class.
class TimingsStatisticsCollector
{
   class RangeStats(TimeSpan start, TimeSpan end)
   {
      long _calls = 0;
      public TimeSpan Start { get; } = start;
      public TimeSpan End { get; } = end;
      public long Calls => _calls;

      public void IncrementIfMatching(TimeSpan time)
      {
         if(Start <= time && End > time)
         {
            Interlocked.Increment(ref _calls);
         }
      }
   }

   readonly RangeStats[] _callStats;
   long _totalCalls;

   readonly MonitorCE _monitor = MonitorCE.WithDefaultTimeout();
   public TimeSpan TotalTime { get; private set; }

   public long TotalCalls => _totalCalls;
   public string Name { get; }

   public TimingsStatisticsCollector(string name, TimeSpan[] rangeDelimiters)
   {
      var rangeBeginnings = rangeDelimiters.ToList().Prepend(TimeSpan.MinValue);
      var rangeEndings = rangeDelimiters.ToList().Append(TimeSpan.MaxValue);

      _callStats = rangeBeginnings.Zip(rangeEndings, (start, end) => new RangeStats(start, end)).ToArray();

      Name = name;
   }

   public void Time(Action action)
   {
      var time = StopwatchCE.TimeExecution(action);
      RegisterCall(time);
   }

   public TResult Time<TResult>(Func<TResult> func)
   {
      TResult? result = default;
      var time = StopwatchCE.TimeExecution(() => result = func());
      RegisterCall(time);
      return result!;
   }

   public async Task TimeAsync(Func<Task> func)
   {
      var time = await StopwatchCE.TimeExecutionAsync(func).CaF();
      RegisterCall(time);
   }

   public async Task<TResult> TimeAsync<TResult>(Func<Task<TResult>> func)
   {
      TResult? result = default;
      var time = await StopwatchCE.TimeExecutionAsync(async () => result = await func().CaF()).CaF();
      RegisterCall(time);
      return result!;
   }

   void RegisterCall(TimeSpan time)
   {
      Interlocked.Increment(ref _totalCalls);
      using(_monitor.TakeUpdateLock()) TotalTime += time;
      // ReSharper disable once ForCanBeConvertedToForeach
      for(var i = 0; i < _callStats.Length; ++i)
      {
         _callStats[i].IncrementIfMatching(time);
      }
   }
}
