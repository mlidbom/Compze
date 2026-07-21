using Compze.Internals.SystemCE.ThreadingCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

// ReSharper disable once ConvertToPrimaryConstructor
namespace Compze.Internals.SystemCE.Private;

///<summary>Simple utility class that calls the supplied action when the instance is disposed. Gets rid of the need to create a ton of small classes to do cleanup.</summary>
sealed class AsyncDisposable : IAsyncDisposable
{
   readonly Func<Task> _dispose;

   public AsyncDisposable(Action dispose) => _dispose = dispose.AsAsync();

   public async ValueTask DisposeAsync() => await _dispose().caf();
}
