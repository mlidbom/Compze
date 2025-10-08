namespace Compze.Utilities.SystemCE.ThreadingCE;

class CombinationUsageGuard(params IUsageGuard[] usageGuards) : IUsageGuard
{
   readonly IUsageGuard[] _usageGuards = usageGuards;

   public void AssertUseValid()
   {
      foreach(var guard in _usageGuards)
      {
         guard.AssertUseValid();
      }
   }
}