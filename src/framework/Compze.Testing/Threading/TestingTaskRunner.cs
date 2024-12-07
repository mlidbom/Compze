using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Compze.SystemCE.LinqCE;
using Compze.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Testing.Threading;

///<summary>
/// Runs and monitors tasks on background threads.
/// Throws <see cref="AggregateException"/> on dispose if any throw exceptions or do not complete within timeout. </summary>
sealed class TestingTaskRunner(TimeSpan timeout) : IDisposable, IAsyncDisposable
{
   readonly List<Task> _monitoredTasks = [];
   readonly TimeSpan _timeout = timeout;

   public static TestingTaskRunner WithTimeout(TimeSpan timeout) => new(timeout);

   public TestingTaskRunner Run(params Action[] tasks)
   {
      tasks.ForEach(action: task => _monitoredTasks.Add(Task.Run(task)));
      return this;
   }

   public void Dispose() => DisposeAsync().WaitUnwrappingException();
   public async ValueTask DisposeAsync() => await WaitForTasksToCompleteAsync();

   async Task WaitForTasksToCompleteAsync() => await Task.WhenAll(_monitoredTasks.ToArray()).WaitAsync(_timeout);
}
