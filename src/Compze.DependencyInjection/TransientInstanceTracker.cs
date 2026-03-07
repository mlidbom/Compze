using System.Collections.Concurrent;
using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;

namespace Compze.DependencyInjection;

class TransientInstanceTracker : IDisposable, IAsyncDisposable
{
   readonly ConcurrentBag<object> _trackedInstances = [];

   internal void Track(object instance)
   {
      if(instance is IDisposable or IAsyncDisposable)
         _trackedInstances.Add(instance);
   }

   public void Dispose()
   {
      while(_trackedInstances.TryTake(out var instance))
      {
         if(instance is IDisposable disposable)
            disposable.Dispose();
         else if(instance is IAsyncDisposable asyncDisposable)
            asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
      }
   }

   public async ValueTask DisposeAsync()
   {
      while(_trackedInstances.TryTake(out var instance))
      {
         if(instance is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync().caf();
         else if(instance is IDisposable disposable)
            disposable.Dispose();
      }
   }
}

class ScopedTransientInstanceTracker : TransientInstanceTracker;
class SingletonTransientInstanceTracker : TransientInstanceTracker;
