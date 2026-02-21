namespace Compze.Utilities.Contracts.UsageGuards;

public class CombinationUsageGuard(params IUsageGuard[] usageGuards) : IUsageGuard
{
   readonly IUsageGuard[] _usageGuards = usageGuards;

   public void EnsureAccessValid()
   {
      foreach(var guard in _usageGuards)
      {
         guard.EnsureAccessValid();
      }
   }
}
