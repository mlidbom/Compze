using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Compze.Underscore;
using Compze.Threading.TasksCE;
using Compze.Utilities.SystemCE.LinqCE;

namespace Compze.Threading.Testing;

///<summary>
/// Runs and monitors tasks on background threads.
/// Throws <see cref="AggregateException"/> on dispose if any throw exceptions or do not complete within timeout. </summary>
public sealed class TestingTaskRunner(TimeSpan timeout) : IDisposable, IAsyncDisposable
{
   readonly List<Task> _monitoredTasks = [];
   readonly TimeSpan _timeout = timeout;

   public static TestingTaskRunner WithTimeout(TimeSpan timeout) => new(timeout);

   public TestingTaskRunner Run(params Action[] tasks)
      => tasks.ForEach(it => _monitoredTasks.Add(TaskCE.Run(it)))._then(this);

   public void Dispose() => DisposeAsync().WaitUnwrappingException();
   public async ValueTask DisposeAsync() => await WaitForTasksToCompleteAsync().caf();

   async Task WaitForTasksToCompleteAsync() => await Task.WhenAll(_monitoredTasks.ToArray()).WaitAsync(_timeout).caf();
}
