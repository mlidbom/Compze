using Compze.Threading.Interprocess._private.ProcessLocks;
using Compze.Threading.Interprocess.ResourceAccess;

namespace Compze.Threading.Interprocess._private.ProcessSemaphores;

///<summary>The files an <see cref="IProcessSemaphore"/> is, in its directory: a lock file for each of its units, a lock file for each waiter slot,<br/>
/// and its <see cref="ProcessSemaphoreQueue"/>, kept in an <see cref="IInterprocessObject{T}"/>.</summary>
sealed class ProcessSemaphoreFiles
{
   ///<summary>Room for about 900 <see cref="WaitingRequest"/>s, of 70 bytes or so each as JSON. The file is this large on disk, but the operating<br/>
   /// system commits memory only for what is written.</summary>
   const int MostBytesOfTheQueue = 64 * 1024;

   internal ProcessSemaphoreFiles(string name, int units, DirectoryInfo directory)
   {
      Name = name;
      Units = units;
      Directory = directory;
   }

   internal string Name { get; }

   internal DirectoryInfo Directory { get; }

   ///<summary>How many units the semaphore has, as this process made it.</summary>
   internal int Units { get; }

   internal ProcessLockFile Unit(int index) => ProcessLockFile.Named($"{Name}.unit-{index}", Directory);

   ///<summary>The lock file of the waiter slot <paramref name="index"/>: a waiting take holds the first one free.</summary>
   internal ProcessLockFile WaiterSlot(int index) => ProcessLockFile.Named($"{Name}.waiter-{index}", Directory);

   ///<summary>Opens the queue, which the takes of every process share. Disposing it closes this process's handles to it.</summary>
   internal IInterprocessObject<ProcessSemaphoreQueue> OpenTheQueue() =>
      IInterprocessObject.NewGlobal($"{Name}.queue", IInterprocessObjectSerializer.Json<ProcessSemaphoreQueue>(), () => new ProcessSemaphoreQueue(),
                                    CorruptionAction.ReplaceContentWithDefaultAndThrow, MostBytesOfTheQueue, Directory);

   ///<summary>True if any of the first <paramref name="units"/> unit files is held.</summary>
   internal bool AnyUnitIsHeld(int units) => Enumerable.Range(0, units).Any(it => Unit(it).IsHeld());

   ///<summary>Who holds the units of the semaphore whose queue is <paramref name="queue"/>, and who waits for them, in the order they came.</summary>
   internal string DescribeHoldersAndWaiters(ProcessSemaphoreQueue queue)
   {
      var holders = Enumerable.Range(0, Math.Max(queue.Units, Units))
                              .Where(it => Unit(it).IsHeld())
                              .GroupBy(it => Unit(it).LastHoldersDescription() ?? "by a holder that hasn't yet written what for")
                              .Select(it => $"{WaitingRequest.UnitsTold(it.Count())} {it.Key}")
                              .ToList();
      var waiters = queue.WaitingRequests(this).Select(it => it.Told(this)).ToList();
      return $"The process semaphore {Name} in {Directory.FullName} has {WaitingRequest.UnitsTold(queue.Units)}. " +
             (holders.Count == 0 ? "None is held. " : $"Held: {string.Join("; ", holders)}. ") +
             (waiters.Count == 0 ? "None waits." : $"Waiting, in the order they came: {string.Join("; ", waiters)}.");
   }
}
