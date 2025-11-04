using System;
using System.Threading;
using Compze.Utilities.Functional;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public partial class MonitorCE
{
   internal UpdateLock EnterUpdateLockWhen(Func<bool> condition) =>
      EnterUpdateLockWhen(InfiniteTimeout, condition);

   internal UpdateLock EnterUpdateLockWhen(TimeSpan conditionTimeout, Func<bool> condition)
   {
      EnterWhen(conditionTimeout, condition);
      return _updateLock;
   }

   internal unit Await(Func<bool> condition) => Await(InfiniteTimeout, condition);

   internal unit Await(TimeSpan conditionTimeout, Func<bool> condition) => Throw<AwaitingConditionTimeoutException>.If(!TryAwait(conditionTimeout, condition));

   internal bool TryAwait(TimeSpan conditionTimeout, Func<bool> condition)
   {
      if(TryEnterWhen(conditionTimeout, condition))
      {
         Exit();
         return true;
      } else
      {
         return false;
      }
   }

   void EnterWhen(TimeSpan conditionTimeout, Func<bool> condition) => Throw<AwaitingConditionTimeoutException>.If(!TryEnterWhen(conditionTimeout, condition));

   bool TryEnterWhen(TimeSpan conditionTimeout, Func<bool> condition)
   {
      if(conditionTimeout == InfiniteTimeout)
      {
         if(!TryEnter(DefaultTimeout)) 
            return false;
         while(!condition()) Wait(InfiniteTimeout);
      } else
      {
         var startTime = DateTime.UtcNow;
         if(!TryEnter(DefaultTimeout)) 
            return false;

         while(!condition())
         {
            var elapsedTime = DateTime.UtcNow - startTime;
            var timeRemaining = conditionTimeout - elapsedTime;
            if(timeRemaining <= TimeSpan.Zero)
            {
               Exit();
               return false;
            }

            Wait(timeRemaining);
         }
      }

      return true;
   }

   void Wait(TimeSpan timeout) => Monitor.Wait(_lockObject, timeout);
}
