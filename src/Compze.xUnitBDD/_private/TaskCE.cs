namespace Compze.xUnitBDD._private;

/// <summary>What Compze.xUnitBDD does with a part of a scenario that is a task, which is built asynchronously.</summary>
static class TaskCE
{
   extension(Task @this)
   {
      /// <summary>
      /// Disposes what the task completes with, once it has, if that is disposable: never the task itself, which can't be<br/>
      /// disposed before it completes. A task that fails, or is canceled, completes with nothing to dispose.
      /// </summary>
      internal async ValueTask DisposeWhatItCompletesWithAsync()
      {
         await @this.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
         switch(@this.IsCompletedSuccessfully ? @this.WhatItCompletedWith : null)
         {
            case IAsyncDisposable asyncDisposable: await asyncDisposable.DisposeAsync().ConfigureAwait(false); break;
            case IDisposable disposable: disposable.Dispose(); break;
         }
      }

      /// <summary>The <see cref="Task{TResult}.Result"/> of a task that has one, once it has completed: null for a task that has none.</summary>
      object? WhatItCompletedWith => @this.GetType().GetProperty(nameof(Task<object>.Result))?.GetValue(@this);
   }
}
