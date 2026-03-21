using System.Collections.Concurrent;

namespace Compze.DependencyInjection.LightInject;

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
            asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
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
