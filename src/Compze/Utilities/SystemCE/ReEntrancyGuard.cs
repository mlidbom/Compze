using System;
using Compze.Utilities.Functional;

namespace Compze.Utilities.SystemCE;

class ReentrancyGuard
{
   bool _isExecuting = false;

   public unit ExecuteIfNotReEntering(Action action) =>
      ExecuteIfNotReEntering(action.AsUnitFunc());

   public unit ExecuteIfNotReEntering(Func<unit> action)
   {
      if(_isExecuting)
         return unit.Value;

      using(ScopedChange.Enter(() => _isExecuting = true, () => _isExecuting = false))
      {
         return action();
      }
   }
}
