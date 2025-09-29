namespace Compze.SystemCE.ThreadingCE;

class CombinationUsageGuard(params ISingleContextUseGuard[] usageGuards) : ISingleContextUseGuard
{
   readonly ISingleContextUseGuard[] _usageGuards = usageGuards;

   public void AssertNoContextChangeOccurred(object guarded)
   {
      foreach(var guard in _usageGuards)
      {
         guard.AssertNoContextChangeOccurred(guarded);
      }
   }
}