using Compze.SystemCE;

namespace Compze.Internals.SystemCE;

public class ReentrancyGuard
{
   bool _isExecuting = false;

   public Unit ExecuteIfNotReEntering(Action action) =>
      ExecuteIfNotReEntering(action.ToFunc());

   Unit ExecuteIfNotReEntering(Func<Unit> action)
   {
      if(_isExecuting)
      {
         return unit;
      }

      using(ScopedChange.Enter(() => _isExecuting = true, () => _isExecuting = false))
      {
         return action();
      }
   }
}
