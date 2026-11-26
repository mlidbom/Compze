using Compze.Contracts;
using Compze.Threading._internal;
using Compze.Threading.Interprocess._private.ProcessLocks;
using Compze.Threading.Interprocess.ResourceAccess;

namespace Compze.Threading.Interprocess._private.ProcessSemaphores;

///<summary>One take of units of an <see cref="IProcessSemaphore"/>, from the moment it is in the semaphore's queue until it holds all its units, or<br/>
/// gives up: its request in the queue, the waiter slot its waiter holds meanwhile, the units it has taken so far, and its wait. Disposing it, unless<br/>
/// it has <see cref="Completed"/>, gives up: it releases what it has taken, and leaves the queue.</summary>
sealed class ProcessSemaphoreTake : IDisposable
{
   ///<summary>The longest a waiting take waits between its looks at the queue and the units. A release, or a change in the queue, ends the wait at<br/>
   /// once; the death of a holder or of a waiting take tells nothing, so a take looks this often for what they leave.</summary>
   static readonly TimeSpan LongestBetweenLooks = TimeSpan.FromSeconds(1);

   readonly ProcessSemaphoreFiles _files;
   readonly string _purpose;
   readonly StartedWait _wait;
   readonly CancellationToken _cancellationToken;
   readonly IInterprocessObject<ProcessSemaphoreQueue> _queue;
   readonly IProcessLockHold _waiterSlot;
   readonly WaitingRequest _request;

   ///<summary>The units taken so far, by their index.</summary>
   readonly Dictionary<int, IProcessLockHold> _unitsTaken = [];

   ///<summary>What it writes in the lock files of the units it takes, all alike: since when it first tried to take one, first in the queue.</summary>
   string? _descriptionOfItsUnitsHolder;

   long _changesSeenAtTheLastLook;
   bool _completed;

   ProcessSemaphoreTake(ProcessSemaphoreFiles files, int units, string purpose, WaitTimeout waitTimeout, CancellationToken cancellationToken)
   {
      _files = files;
      _purpose = purpose;
      _wait = StartedWait.Begin(waitTimeout);
      _cancellationToken = cancellationToken;
      var waitersDescription = HoldersDescription.OfThisProcessHoldingItFor(purpose);
      _waiterSlot = TheFirstFreeWaiterSlot(files, waitersDescription, out var waiterSlotIndex);
      _request = new WaitingRequest { Id = Guid.NewGuid(), Units = units, WaiterSlot = waiterSlotIndex };
      try
      {
         _queue = files.OpenTheQueue();
      }
      catch //Cleanup, not handling: a take that fails to begin must hold nothing.
      {
         _waiterSlot.Dispose();
         throw;
      }
   }

   ///<summary>The take of <paramref name="units"/> units of the semaphore of <paramref name="files"/> for <paramref name="purpose"/>, begun: its request<br/>
   /// last in the queue. Its wait may last <paramref name="waitTimeout"/>, and throws an <see cref="OperationCanceledException"/> once<br/>
   /// <paramref name="cancellationToken"/> is cancelled, as beginning does if it already is.</summary>
   internal static ProcessSemaphoreTake Begin(ProcessSemaphoreFiles files, int units, string purpose, WaitTimeout waitTimeout, CancellationToken cancellationToken)
   {
      cancellationToken.ThrowIfCancellationRequested();
      var take = new ProcessSemaphoreTake(files, units, purpose, waitTimeout, cancellationToken);
      try
      {
         var request = take._request;
         take._queue.Update(it => it.Add(request, files));
         return take;
      }
      catch //Cleanup, not handling: a take that never got into the queue must hold nothing, nor leave anything open.
      {
         take.Dispose();
         throw;
      }
   }

   ///<summary>Looks at the queue, and at the units if its request is first: takes those that are free, and returns true once it holds them all.<br/>
   /// A request ahead of its own whose waiter has died, it removes from the queue, and looks again.</summary>
   internal bool HoldsAllItsUnitsAfterALook()
   {
      while(true)
      {
         var (first, changes) = _queue.Read(it => (it.First(_request), it.Changes));
         _changesSeenAtTheLastLook = changes;
         if(first.Id == _request.Id)
         {
            TakeTheUnitsThatAreFree();
            return _unitsTaken.Count == _request.Units;
         }

         if(_files.WaiterSlot(first.WaiterSlot).IsHeld()) return false;

         _queue.Update(it => it.Remove(first)); //Its waiter has died: a waiter holds its waiter slot until its request has left the queue.
      }
   }

   ///<summary>Waits until the queue has changed since the last look, as a release changes it, but no longer than until the next look is due: or<br/>
   /// returns false at once if the wait has ended.</summary>
   internal bool WaitedForTheNextLook()
   {
      if(PauseBeforeTheNextLook() is not {} pause) return false;
      _queue.TryAwait(it => it.Changes != _changesSeenAtTheLastLook, _cancellationToken, pause);
      return true;
   }

   ///<summary>As <see cref="WaitedForTheNextLook"/>, holding no thread while it waits.</summary>
   internal async Task<bool> WaitedForTheNextLookAsync()
   {
      if(PauseBeforeTheNextLook() is not {} pause) return false;
      await _queue.TryAwaitAsync(it => it.Changes != _changesSeenAtTheLastLook, _cancellationToken, pause).ConfigureAwait(false);
      return true;
   }

   ///<summary>The units, held, once it holds them all: its request leaves the queue, which tells the take after it, and its waiter slot is released.</summary>
   internal IProcessSemaphoreHold Completed()
   {
      State.Assert(_unitsTaken.Count == _request.Units);
      _queue.Update(it => it.Remove(_request));
      _waiterSlot.Dispose();
      _completed = true;
      return new ProcessSemaphoreHold([.._unitsTaken.Values], _queue);
   }

   ///<summary>Gives up, unless it has <see cref="Completed"/>: releases the units it has taken, removes its request from the queue, which tells the<br/>
   /// take after it, and releases its waiter slot.</summary>
   public void Dispose()
   {
      if(_completed) return;

      foreach(var unit in _unitsTaken.Values) unit.Dispose();
      _queue.Update(it => it.Remove(_request));
      _waiterSlot.Dispose();
      _queue.Dispose();
   }

   void TakeTheUnitsThatAreFree()
   {
      for(var index = 0; index < _files.Units && _unitsTaken.Count < _request.Units; index++)
      {
         if(_unitsTaken.ContainsKey(index)) continue;
         _descriptionOfItsUnitsHolder ??= HoldersDescription.OfThisProcessHoldingItFor(_purpose);
         var unit = _files.Unit(index).TryHold(_descriptionOfItsUnitsHolder);
         if(unit != null) _unitsTaken.Add(index, unit);
      }
   }

   ///<summary>Holds the first waiter slot that no other waiter holds, and returns its hold, and its <paramref name="index"/>.</summary>
   static IProcessLockHold TheFirstFreeWaiterSlot(ProcessSemaphoreFiles files, string waitersDescription, out int index)
   {
      for(index = 0;; index++)
      {
         var waiterSlot = files.WaiterSlot(index).TryHold(waitersDescription);
         if(waiterSlot != null) return waiterSlot;
      }
   }

   ///<summary>What is left of the wait, but no more than <see cref="LongestBetweenLooks"/>: null once the wait has ended.</summary>
   WaitTimeout? PauseBeforeTheNextLook() => _wait.HasExpired ? null : new WaitTimeout(_wait.TimeRemainingButNoMoreThan(LongestBetweenLooks));
}
