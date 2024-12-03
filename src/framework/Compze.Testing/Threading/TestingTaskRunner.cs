using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Compze.Contracts;
using Compze.SystemCE.LinqCE;
using Compze.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Testing.Threading;

///<summary>
/// Runs and monitors tasks on background threads.
/// Throws <see cref="AggregateException"/> on dispose if any throw exceptions or do not complete within timeout. </summary>
public sealed class TestingTaskRunner(TimeSpan timeout) : IDisposable
{
   readonly List<Task> _monitoredTasks = [];
   readonly TimeSpan _timeout = timeout;

   public static TestingTaskRunner WithTimeout(TimeSpan timeout) => new(timeout);

   public void Monitor(IEnumerable<Task> tasks) => Monitor(tasks.ToArray());
   public void Monitor(params Task[] tasks) => _monitoredTasks.AddRange(tasks);

   public TestingTaskRunner Run(IEnumerable<Action> tasks) => Run(tasks.ToArray());

   public TestingTaskRunner Run(params Action[] tasks)
   {
      tasks.ForEach(action: task => _monitoredTasks.Add(TaskCE.Run($"{nameof(TestingTaskRunner)}_Task", task)));
      return this;
   }

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
