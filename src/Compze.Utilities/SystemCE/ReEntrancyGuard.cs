using System;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE.ActionFuncHarmonization;

namespace Compze.Utilities.SystemCE;

public class ReentrancyGuard
{
   bool _isExecuting = false;
   bool _reentryWasAttempted = false;

   public unit ExecuteIfNotReEntering(Action action) =>
      ExecuteIfNotReEntering(action.AsFunc());

   public unit ExecuteIfNotReEntering(Func<unit> action)
   {
      if(_isExecuting)
      {
         _reentryWasAttempted = true;
         return unit.Value;
      }

      using(ScopedChange.Enter(() => _isExecuting = true, () => _isExecuting = false))
      {
         return action();
      }
   }

   public bool GetAndClearReentryWasAttempted()
   {
      using(new Disposable(() => _reentryWasAttempted = false))
      {
         return _reentryWasAttempted;
      }
   }
}
