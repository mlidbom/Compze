using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Threading.ResourceAccess;

namespace Compze.Tessaging._private.SystemCE.ThreadingCE;

//todo: whether this belongs in this project is an open question
interface ITaskRunner
{
   void Run(string taskName, Action action);
   ///<summary>Runs <paramref name="asyncAction"/> as a tracked background task: started on a thread-pool thread, tracked until<br/>
   /// the whole async work completes, its failure reported through the background-exception reporter.</summary>
   void Run(string taskName, Func<Task> asyncAction);
   ///<summary>Runs <paramref name="threadLoop"/> on a thread of its own, named <paramref name="threadName"/>, and returns a task that<br/>
   /// completes when the thread ends, so the end can be awaited without holding a thread. A failure is reported through the<br/>
   /// background-exception reporter, so the task always completes successfully.</summary>
   Task RunOnNamedThread(string threadName, ThreadStart threadLoop, ThreadPriority priority = ThreadPriority.Normal);
}

static class TaskRunnerRegistrar
{
   public static IComponentRegistrar TaskRunner(this IComponentRegistrar registrar)
      => registrar.Register(TaskRunnerCore.RegisterWith);

   class TaskRunnerCore : ITaskRunner, IDisposable
   {
      public static void RegisterWith(IComponentRegistrar registrar)
         => registrar.Register(Singleton.For<ITaskRunner>().CreatedBy((IBackgroundExceptionReporter exceptionReporter) => new TaskRunnerCore(exceptionReporter)));

      readonly IBackgroundExceptionReporter _exceptionReporter;
      readonly IThreadShared<List<Thread>> _threads = IThreadShared.New(new List<Thread>());
      readonly IAwaitableThreadShared<HashSet<Task>> _inProgressTasks = IAwaitableThreadShared.New(new HashSet<Task>());

      TaskRunnerCore(IBackgroundExceptionReporter exceptionReporter) => _exceptionReporter = exceptionReporter;

      public void Run(string taskName, Action action) => Run(taskName, () =>
      {
         action();
         return Task.CompletedTask;
      });

      public void Run(string taskName, Func<Task> asyncAction)
      {
         var task = TaskCE.Run(async () =>
         {
            try
            {
               await asyncAction().caf();
            }
#pragma warning disable CA1031 //This is specifically designed for making sure that exceptions thrown in places where they cannot be surfaced directly, are not just ignored
            catch(Exception exception)
            {
               try
               {
                  this.Log().Error(exception, $"TaskRunner caught an exception while running task: {taskName}");
                  throw new TaskRunnerException(exception, $"Running task {taskName} threw exception");
               }
               catch(TaskRunnerException taskRunnerException)
               {
                  _exceptionReporter.ReportException(taskRunnerException);
               }
            }
#pragma warning restore CA1031
         });

         _inProgressTasks.Update(it => it.Add(task));
         task.ContinueWithCE(_ => _inProgressTasks.Update(it => it.Remove(task))); //While surprising to me, completedTask and task are NOT the same object.
      }

      public Task RunOnNamedThread(string threadName, ThreadStart threadLoop, ThreadPriority priority = ThreadPriority.Normal)
      {
         //Continuations run asynchronously, so code awaiting the thread's end never runs on the ending thread itself.
         var threadEnded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
         var thread = new Thread(() =>
                      {
                         try
                         {
                            threadLoop.Invoke();
                         }
                         catch(Exception exception) when(exception is OperationCanceledException or ThreadInterruptedException or ThreadAbortException)
                         {
                            this.Log().Info($"Thread: {threadName} is terminating because it received a: {exception.GetType().Name}.");
                         }
#pragma warning disable CA1031 //This is specifically designed for making sure that exceptions thrown in places where they cannot be surfaced directly, are not just ignored
                         catch(Exception exception)
                         {
#pragma warning restore CA1031
                            _exceptionReporter.ReportException(exception);
                         }
                         finally
                         {
                            threadEnded.SetResult();
                         }
                      })
                      {
                         Name = threadName,
                         Priority = priority
                      };

         _threads.Locked(it => it.Add(thread));
         thread.Start();
         return threadEnded.Task;
      }

      readonly CancellationTokenSource _cancellationTokenSource = new();

      public void Dispose()
      {
         var threads = _threads.Locked(threads => threads.ToArray());
         threads.Where(it => it.IsAlive)
                .ForEach(it => it.Interrupt());
         threads.Where(it => it.IsAlive)
                .ForEach(it => it.Join());
         _inProgressTasks.Await(it => !it.Any());

         _cancellationTokenSource.Dispose();
      }
   }
}
