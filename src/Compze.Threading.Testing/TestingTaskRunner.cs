using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Threading.Testing;

///<summary>
/// Runs and monitors the actors of a test: synchronous actions on dedicated threads, async functions on the thread pool.<br/>
/// On dispose, rethrows the exception of the first actor that failed, or throws a <see cref="TimeoutException"/> if they don't all complete within the timeout.</summary>
///<remarks>A test's actors typically block, at a gate or in the code under test. A synchronous action that blocks on a thread-pool thread parks a thread<br/>
/// the code under test may need to run on, so it gets a dedicated thread instead. An async function holds no thread while it awaits, so it runs on the thread pool.</remarks>
///<remarks>The runner observes each actor's task, so <see cref="Start(Action)"/> returns none. A test that awaits one actor itself, such as with a timeout of<br/>
/// its own, runs it with <see cref="RunAsync(Action)"/>, which the runner observes too.</remarks>
public sealed class TestingTaskRunner(TimeSpan timeout) : IDisposable, IAsyncDisposable
{
   readonly List<Task> _monitoredTasks = [];
   readonly TimeSpan _timeout = timeout;

   ///<summary>Returns an instance that throws an exception on <see cref="Dispose"/> if all tasks do not complete within <paramref name="timeout"/></summary>
   public static TestingTaskRunner WithTimeout(TimeSpan timeout) => new(timeout);

   ///<summary>Starts the supplied action on a dedicated thread, outside the thread pool. Throws its exception, if it fails, on <see cref="Dispose"/>.</summary>
   public void Start(Action action) => _ = RunAsync(action);

   ///<summary>Like <see cref="Start(Action)"/>, and returns the action's <see cref="Task"/>, for a test that awaits it.</summary>
   public Task RunAsync(Action action)
   {
      var actor = TaskCE.RunOnDedicatedThreadAsync(action);
      _monitoredTasks.Add(actor);
      return actor;
   }

   ///<summary>Starts each of the supplied actions on a dedicated thread, outside the thread pool. Throws the exception of the first that fails on <see cref="Dispose"/>.</summary>
   public void Start(params Action[] actions)
   {
      foreach(var action in actions) Start(action);
   }

   ///<summary>Starts the supplied async function on the thread pool. Throws its exception, if it fails, on <see cref="Dispose"/>.</summary>
   public void Start(Func<Task> asyncFunction) => _ = RunAsync(asyncFunction);

   ///<summary>Like <see cref="Start(Func{Task})"/>, and returns the function's <see cref="Task"/>, for a test that awaits it.</summary>
   public Task RunAsync(Func<Task> asyncFunction)
   {
      var actor = TaskCE.RunAsync(asyncFunction);
      _monitoredTasks.Add(actor);
      return actor;
   }

   ///<summary>Starts each of the supplied async functions on the thread pool. Throws the exception of the first that fails on <see cref="Dispose"/>.</summary>
   public void Start(params Func<Task>[] asyncFunctions)
   {
      foreach(var asyncFunction in asyncFunctions) Start(asyncFunction);
   }

   ///<summary>Waits for all the registered tasks to complete and throws an exception if any are failed or the timeout expires.</summary>
   public void Dispose()
   {
      //todo:urgent: a synchronous wait on asynchronous work, through WaitUnwrappingException, which VSTHRD002 can't see: it
      //   holds this thread, and stalls while the thread pool is busy. See
      //   dev_docs/TODO/synchronous-paths-that-need-a-free-thread-pool-thread.md (Magnus, 2026-11-24).
      DisposeAsync().WaitUnwrappingException();
   }
   ///<summary>Waits for all the registered tasks to complete and throws an exception if any are failed or the timeout expires.</summary>
   public async ValueTask DisposeAsync() => await WaitForTasksToCompleteAsync().caf();

   async Task WaitForTasksToCompleteAsync()
   {
      //refactor: only the first failed actor's exception surfaces, and the other actors' failures are lost. Throwing them all, as an
      //   AggregateException when more than one failed, would show every failure of a run.
      await Task.WhenAll(_monitoredTasks.ToArray()).WaitAsync(_timeout).caf();
   }
}
