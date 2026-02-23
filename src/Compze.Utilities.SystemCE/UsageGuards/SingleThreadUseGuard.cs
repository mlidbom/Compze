using System.Threading;

namespace Compze.Utilities.SystemCE.UsageGuards;

///<summary>Ensures that guarded components are used within one thread only.</summary>
public class SingleThreadUseGuard : UsageGuard
{
   readonly object _guarded;
   readonly Thread _owningThread;

   ///<summary>Default constructor associates the instance with the current thread.</summary>
   public SingleThreadUseGuard(object guarded)
   {
      _guarded = guarded;
      _owningThread = Thread.CurrentThread;
   }

   ///<summary>Throws a <see cref="MultiThreadedUseException"/> if the current thread is different from the one that the instance was constructed in.</summary>
   protected override void InternalAssertUsageAllowed()
   {
      if (Thread.CurrentThread != _owningThread)
      {
         throw new MultiThreadedUseException(_guarded, _owningThread, Thread.CurrentThread);
      }
   }
}
