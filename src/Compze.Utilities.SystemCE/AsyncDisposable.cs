using System;
using System.Threading.Tasks;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Compze.Utilities.SystemCE.ThreadingCE;

namespace Compze.Utilities.SystemCE;

///<summary>Simple utility class that calls the supplied action when the instance is disposed. Gets rid of the need to create a ton of small classes to do cleanup.</summary>
sealed class AsyncDisposable : IAsyncDisposable
{
   readonly Func<Task> _dispose;

   public AsyncDisposable(Action dispose) => _dispose = dispose.AsAsync();

   public AsyncDisposable(Func<Task> dispose) => _dispose = dispose;

   public AsyncDisposable(Func<ValueTask> dispose) => _dispose = () => dispose().AsTask();

   public async ValueTask DisposeAsync() => await _dispose().caf();

   public static readonly IAsyncDisposable NullOp = new AsyncDisposable(ActionCE.NullOp);
}