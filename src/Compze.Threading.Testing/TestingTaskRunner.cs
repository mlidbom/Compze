using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Threading.Testing;

///<summary>
/// Runs and monitors the actors of a test: synchronous actions on dedicated threads, async functions on the thread pool.<br/>
/// On dispose, rethrows the exception of the first actor that failed, or throws a <see cref="TimeoutException"/> if they don't all complete within the timeout.</summary>
///<remarks>A test's actors typically block, at a gate or in the code under test. A synchronous action that blocks on a thread-pool thread parks a thread<br/>
/// the code under test may need to run on, so it gets a dedicated thread instead. An async function holds no thread while it awaits, so it runs on the thread pool.</remarks>
public sealed class TestingTaskRunner(TimeSpan timeout) : IDisposable, IAsyncDisposable
{
   readonly List<Task> _monitoredTasks = [];
   readonly TimeSpan _timeout = timeout;

   ///<summary>Returns an instance that throws an exception on <see cref="Dispose"/> if all tasks do not complete within <paramref name="timeout"/></summary>
   public static TestingTaskRunner WithTimeout(TimeSpan timeout) => new(timeout);

   ///<summary>Starts the supplied action on a dedicated thread, outside the thread pool, and returns its <see cref="Task"/>. Tracks all the started tasks and throws an exception on <see cref="Dispose"/> if any are failed</summary>
   public Task Run(Action action) => Monitored(TaskCE.RunOnDedicatedThread(action));

   ///<summary>Starts each of the supplied actions on a dedicated thread, outside the thread pool, and returns their tasks. Tracks all the started tasks and throws an exception on <see cref="Dispose"/> if any are failed</summary>
   public IReadOnlyList<Task> Run(params Action[] actions) => [..actions.Select(Run)];

   ///<summary>Starts the supplied async function on the thread pool and returns its <see cref="Task"/>. Tracks all the started tasks and throws an exception on <see cref="Dispose"/> if any are failed</summary>
   public Task Run(Func<Task> asyncFunction) => Monitored(TaskCE.Run(asyncFunction));

   ///<summary>Starts each of the supplied async functions on the thread pool and returns their tasks. Tracks all the started tasks and throws an exception on <see cref="Dispose"/> if any are failed</summary>
   public IReadOnlyList<Task> Run(params Func<Task>[] asyncFunctions) => [..asyncFunctions.Select(Run)];

   Task Monitored(Task task)
   {
      _monitoredTasks.Add(task);
      return task;
   }

   ///<summary>Waits for all the registered tasks to complete and throws an exception if any are failed or the timeout expires.</summary>
   public void Dispose() => DisposeAsync().WaitUnwrappingException();
   ///<summary>Waits for all the registered tasks to complete and throws an exception if any are failed or the timeout expires.</summary>
   public async ValueTask DisposeAsync() => await WaitForTasksToCompleteAsync().caf();

   async Task WaitForTasksToCompleteAsync() => await Task.WhenAll(_monitoredTasks.ToArray()).WaitAsync(_timeout).caf();
}
