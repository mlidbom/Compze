using System;
using System.Threading;
using Compze.Utilities.Functional;

namespace Compze.Utilities.SystemCE.ThreadingCE;

public class RunJustOnce
{
   readonly Action _action;
   double _hasPerformedLazySetup;

   public RunJustOnce(Action action) => _action = action;
   public RunJustOnce(Func<unit> action) => _action = () => action();

   public void RunIfNotExecutedBefore()
   {
      if(Interlocked.CompareExchange(ref _hasPerformedLazySetup, 1, 0) == 0)
      {
         _action();
      }
   }
}
