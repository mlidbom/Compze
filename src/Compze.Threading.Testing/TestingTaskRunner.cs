using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;

namespace Compze.Threading.Testing;

///<summary>
/// Runs and monitors tasks on background threads.
/// Throws <see cref="AggregateException"/> on dispose if any throw exceptions or do not complete within timeout. </summary>
public sealed class TestingTaskRunner(TimeSpan timeout) : IDisposable, IAsyncDisposable
{
   readonly List<Task> _monitoredTasks = [];
   readonly TimeSpan _timeout = timeout;

   public static TestingTaskRunner WithTimeout(TimeSpan timeout) => new(timeout);

   public Task Run(Action action)
   {
      var task = TaskCE.Run(action);
      _monitoredTasks.Add(task);
      return task;
   }

   public IReadOnlyList<Task> Run(params Action[] tasks) => tasks.Select(Run).ToList();

   public void Dispose() => DisposeAsync().WaitUnwrappingException();
   public async ValueTask DisposeAsync() => await WaitForTasksToCompleteAsync().caf();

   async Task WaitForTasksToCompleteAsync() => await Task.WhenAll(_monitoredTasks.ToArray()).WaitAsync(_timeout).caf();
}
