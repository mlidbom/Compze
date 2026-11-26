using Compze.Threading.Interprocess.ResourceAccess;

namespace Compze.Threading.Interprocess._private.ProcessSemaphores;

///<summary>Units of an <see cref="IProcessSemaphore"/>, held: the holds of their lock files, and the semaphore's queue, which disposing it tells of<br/>
/// the release, so that the take first in it takes them at once.</summary>
sealed class ProcessSemaphoreHold : IProcessSemaphoreHold
{
   readonly IReadOnlyList<IProcessLockHold> _units;
   readonly IInterprocessObject<ProcessSemaphoreQueue> _queue;
   int _released;

   internal ProcessSemaphoreHold(IReadOnlyList<IProcessLockHold> units, IInterprocessObject<ProcessSemaphoreQueue> queue)
   {
      _units = units;
      _queue = queue;
   }

   public int Units => _units.Count;

   public void Dispose()
   {
      if(Interlocked.Exchange(ref _released, 1) == 1) return;

      foreach(var unit in _units) unit.Dispose();
      _queue.Update(it => it.UnitsWereReleased());
      _queue.Dispose();
   }
}
