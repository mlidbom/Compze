using Compze.Threading.Interprocess;

namespace Compze.Threading.Specifications.ICriticalSection_.Infrastructure;

///<summary>Testing extensions for <see cref="IMutex"/> to support abandoned mutex scenarios.</summary>
static class IMutexAbandonedExtensions
{
   extension(IMutex @this)
   {
      ///<summary>Takes the lock on a background thread that exits without releasing it, leaving the mutex in the "abandoned" state. After this method returns, the next acquisition attempt will receive an <see cref="AbandonedMutexException"/>.</summary>
      public void Abandon()
      {
         var lockAcquired = new ManualResetEventSlim(false);
         var thread = new Thread(() =>
         {
            @this.TakeLock();
            lockAcquired.Set();
         });
         thread.Start();
         lockAcquired.Wait();
         thread.Join();
      }

      ///<summary>Takes the lock on a background thread and holds it until the returned <see cref="Action"/> is invoked. Invoking the action causes the holding thread to exit without releasing the lock (abandoning the mutex) and blocks until the thread has fully terminated.</summary>
      public Action HoldLockUntilAbandoned()
      {
         var lockAcquired = new ManualResetEventSlim(false);
         var abandonSignal = new ManualResetEventSlim(false);
         var thread = new Thread(() =>
         {
            @this.TakeLock();
            lockAcquired.Set();
            abandonSignal.Wait();
         });
         thread.Start();
         lockAcquired.Wait();
         return () =>
         {
            abandonSignal.Set();
            thread.Join();
         };
      }
   }
}
