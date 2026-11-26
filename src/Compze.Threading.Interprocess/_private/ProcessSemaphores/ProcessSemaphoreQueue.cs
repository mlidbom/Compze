using System.Text.Json.Serialization;
using Compze.Contracts;

namespace Compze.Threading.Interprocess._private.ProcessSemaphores;

///<summary>What the processes using an <see cref="IProcessSemaphore"/> share, in an <see cref="ResourceAccess.IInterprocessObject{T}"/>: how many<br/>
/// units it has, and the <see cref="WaitingRequest"/>s, in the order they came. Who holds units isn't here: a unit is held while its lock file is.</summary>
///<remarks>It is kept as JSON: <see cref="JsonIncludeAttribute"/> marks what is kept.</remarks>
sealed class ProcessSemaphoreQueue
{
   ///<summary>How many units the semaphore has: 0 until it is first taken.</summary>
   [JsonInclude] internal int Units { get; private set; }

   [JsonInclude] List<WaitingRequest> Requests { get; set; } = [];

   ///<summary>How many times it has changed, and units have been released: a waiting take waits for it to change.</summary>
   [JsonInclude] internal long Changes { get; private set; }

   ///<summary>Adds <paramref name="request"/> last, as the semaphore of <paramref name="files"/>, whose number of units its waiter takes. The waiter<br/>
   /// holds the request's waiter slot, so a request already in the queue with that slot is one whose waiter died: it is forgotten.</summary>
   internal void Add(WaitingRequest request, ProcessSemaphoreFiles files)
   {
      Requests.RemoveAll(it => it.WaiterSlot == request.WaiterSlot);
      TakeTheUnitsOf(files);
      Requests.Add(request);
      Changes++;
   }

   ///<summary>The first request: the one that takes units. Asserts that <paramref name="ownRequest"/>, whose waiter asks, is in the queue: only<br/>
   /// its waiter removes it while it lives, unless the queue's file was corrupt or deleted.</summary>
   internal WaitingRequest First(WaitingRequest ownRequest)
   {
      State.Assert(Requests.Any(it => it.Id == ownRequest.Id), () => $"The queue of a process semaphore lost a request for {WaitingRequest.UnitsTold(ownRequest.Units)}, which only its waiter removes while it waits: the queue's file was corrupt, or deleted.");
      return Requests[0];
   }

   internal void Remove(WaitingRequest request)
   {
      Requests.RemoveAll(it => it.Id == request.Id);
      Changes++;
   }

   internal void UnitsWereReleased() => Changes++;

   ///<summary>The requests whose waiters still wait, in the order they came: a request whose waiter slot is free is one whose waiter died.</summary>
   internal IReadOnlyList<WaitingRequest> WaitingRequests(ProcessSemaphoreFiles files) => [..Requests.Where(it => files.WaiterSlot(it.WaiterSlot).IsHeld())];

   ///<summary>Makes the semaphore have the units of <paramref name="files"/>, the number its new waiter takes, if they differ and no unit is held or<br/>
   /// waited for: else throws.</summary>
   void TakeTheUnitsOf(ProcessSemaphoreFiles files)
   {
      if(Units == files.Units) return;
      Requests.RemoveAll(it => !files.WaiterSlot(it.WaiterSlot).IsHeld());
      if(Units != 0 && (Requests.Count > 0 || files.AnyUnitIsHeld(Units)))
         throw new InvalidOperationException($"Can't take units of the process semaphore {files.Name} in {files.Directory.FullName} as one of {files.Units} units: it has {Units}, which are held or waited for. " +
                                             $"All who use a semaphore at once must use the same number of units. {files.DescribeHoldersAndWaiters(this)}");
      Units = files.Units;
   }
}
