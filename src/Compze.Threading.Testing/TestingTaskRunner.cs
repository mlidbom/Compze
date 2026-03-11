using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;

namespace Compze.Threading.Testing;

///<summary>
/// Runs and monitors tasks on background threads.
/// Throws <see cref="AggregateException"/> on dispose if any throw exceptions or do not complete within timeout. </summary>
public sealed class TestingTaskRunner(TimeSpan timeout) : IDisposable, IAsyncDisposable
{
   readonly List<Task> _monitoredTasks = [];
   readonly TimeSpan _timeout = timeout;

   ///<summary>Returns an instance that throws an exception on <see cref="Dispose"/> if all tasks do not complete within <paramref name="timeout"/></summary>
   public static TestingTaskRunner WithTimeout(TimeSpan timeout) => new(timeout);

   ///<summary>Starts the supplied action as a background <see cref="Task"/> and returns that task. Tracks all the started tasks and throws an exception on <see cref="Dispose"/> if any are failed</summary>
   public Task Run(Action action)
   {
      var task = TaskCE.Run(action);
      _monitoredTasks.Add(task);
      return task;
   }

   ///<summary>Starts the supplied actions as a background <see cref="Task"/> and returns the list of tasks. Tracks all the started tasks and throws an exception on <see cref="Dispose"/> if any are failed</summary>
   public IReadOnlyList<Task> Run(params Action[] tasks) => tasks.Select(Run).ToList();

   ///<summary>Waits for all the registered tasks to complete and throws an exception if any are failed or the timeout expires.</summary>
   public void Dispose() => DisposeAsync().WaitUnwrappingException();
   ///<summary>Waits for all the registered tasks to complete and throws an exception if any are failed or the timeout expires.</summary>
   public async ValueTask DisposeAsync() => await WaitForTasksToCompleteAsync().caf();

   async Task WaitForTasksToCompleteAsync() => await Task.WhenAll(_monitoredTasks.ToArray()).WaitAsync(_timeout).caf();
}
