using System;
using System.Threading.Tasks;

namespace Compze.SystemCE.ThreadingCE.TasksCE;

class AsyncTaskCompletionSource<TResult>
{
   readonly TaskCompletionSource<TResult> _completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

   public Task<TResult> Task => _completionSource.Task;

   public void ScheduleContinuation(TResult result) => _completionSource.SetResult(result);
   public void ScheduleException(Exception exception) => _completionSource.SetException(exception);
}