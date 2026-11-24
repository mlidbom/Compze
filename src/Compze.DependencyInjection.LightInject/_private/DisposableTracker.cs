using System.Collections.Concurrent;

namespace Compze.DependencyInjection.LightInject._private;

sealed class DisposableTracker
{
   readonly ConcurrentBag<object> _trackedInstances = new();

   public void Track(object instance)
   {
      if(instance is IDisposable or IAsyncDisposable)
         _trackedInstances.Add(instance);
   }

   public void DisposeAll()
   {
      foreach(var item in _trackedInstances)
      {
         if(item is IAsyncDisposable asyncDisposable)
            //todo:urgent: blocks on DisposeAsync. Decided 2026-11-14: a synchronous Dispose throws here instead, as
            //   dev_docs/TODO/container-disposal-of-async-only-components.md says (Magnus, 2026-11-24).
#pragma warning disable VSTHRD002 // A synchronous Dispose of a component that is only asynchronously disposable.
            asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
         else
            (item as IDisposable)?.Dispose();
      }
   }

   public async ValueTask DisposeAllAsync()
   {
      foreach(var item in _trackedInstances)
      {
         if(item is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
         else
            (item as IDisposable)?.Dispose();
      }
   }
}
