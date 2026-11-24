using System.Threading.Channels;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ReactiveCE;

namespace Compze.Teventive.MemoryImageStateStore._private;

///<summary>Tells the subscribers of <see cref="IMemoryImageStateStore.ChangesCommittedByOtherMemoryImages"/> what the store found that other memory<br/>
/// images committed: in the order found, one batch at a time, on a task of its own, so that no subscriber runs on the thread that found<br/>
/// them, such as a unit of work's, and a slow subscriber holds up no one but the batches after it.</summary>
sealed class ChangesCommittedByOtherMemoryImagesNotifier : IObservable<ChangesFoundInStorage>
{
   readonly SimpleObservable<ChangesFoundInStorage> _subscribers = new();
   readonly Channel<ChangesFoundInStorage> _found = Channel.CreateUnbounded<ChangesFoundInStorage>(new UnboundedChannelOptions { SingleReader = true });
   readonly Task _telling;

   internal ChangesCommittedByOtherMemoryImagesNotifier() => _telling = Task.Run(TellTheSubscribersAsync);

   ///<summary>Queues <paramref name="changes"/> for the subscribers, unless nothing changed.</summary>
   internal void Tell(ChangesFoundInStorage changes)
   {
      if(!changes.IsEmpty) _found.Writer.TryWrite(changes);
   }

   public IDisposable Subscribe(IObserver<ChangesFoundInStorage> observer) => _subscribers.Subscribe(observer);

   async Task TellTheSubscribersAsync()
   {
      await foreach(var changes in _found.Reader.ReadAllAsync().ConfigureAwait(false))
      {
         try
         {
            _subscribers.OnNext(changes);
         }
#pragma warning disable CA1031 //The task's entry point, which surfaces a subscriber's failure in the log and goes on telling the others the next batch.
         catch(Exception exception)
#pragma warning restore CA1031
         {
            this.Log().Error(exception, "A subscriber of a memory image state store's ChangesCommittedByOtherMemoryImages threw.");
         }
      }
   }

   ///<summary>Tells what is queued, then stops.</summary>
   internal void Stop()
   {
      _found.Writer.TryComplete();
      //todo:urgent: blocks until a thread-pool task ends, so the disposal stalls while the pool is busy. The store
      //   becomes IAsyncDisposable with dev_docs/TODO/container-disposal-of-async-only-components.md (Magnus, 2026-11-24).
#pragma warning disable VSTHRD002 // The store's synchronous Dispose waits for its background task to end.
      _telling.Wait();
#pragma warning restore VSTHRD002
   }
}
