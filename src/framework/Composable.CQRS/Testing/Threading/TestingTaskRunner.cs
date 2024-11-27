﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.SystemCE.LinqCE;
using Composable.SystemCE.ThreadingCE.TasksCE;

namespace Composable.Testing.Threading;

///<summary>
/// Runs and monitors tasks on background threads.
/// Throws <see cref="AggregateException"/> on dispose if any throw exceptions or do not complete within timeout. </summary>
public sealed class TestingTaskRunner : IDisposable
{
   readonly List<Task> _monitoredTasks = [];
   readonly TimeSpan _timeout;

   public static TestingTaskRunner WithTimeout(TimeSpan timeout) => new(timeout);

   public TestingTaskRunner(TimeSpan timeout) => _timeout = timeout;

   public void Monitor(IEnumerable<Task> tasks) => Monitor(tasks.ToArray());
   public void Monitor(params Task[] task) => _monitoredTasks.AddRange(task);

   public void StartTimes(int times, Func<Task> task) => Monitor(1.Through(times).Select(selector: _ => task()));
   public void StartTimes(int times, Func<int, Task> task) => Monitor(1.Through(times).Select(task));

   public TestingTaskRunner Start(IEnumerable<Action> tasks) => Start(tasks.ToArray());
   public TestingTaskRunner Start(params Action[] tasks)
   {
      tasks.ForEach(action: task => _monitoredTasks.Add(TaskCE.Run($"{nameof(TestingTaskRunner)}_Task", task)));
      return this;
   }

   public void StartTimes(int times, Action task) => Start(1.Through(times).Select(selector: _ => task));
   public void StartTimes(int times, Action<int> task) => Start(1.Through(times).Select<int, Action>(selector: index => () => task(index)));

   public void Dispose() => WaitForTasksToComplete();

   public void WaitForTasksToComplete()
   {
      if(!Task.WaitAll(_monitoredTasks.ToArray(), _timeout))
      {
         var exceptions = _monitoredTasks.Where(predicate: it => it.IsFaulted)
                                         .Select(selector: it => Contract.ReturnNotNull(it.Exception))
                                         .ToList();

         if(exceptions.Any()) throw new AggregateException($"Tasks failed to complete within timeout {_timeout} and there were exceptions in tasks", exceptions);

         throw new AggregateException($"Tasks failed to completed within timeout: {_timeout}");
      }
   }
}