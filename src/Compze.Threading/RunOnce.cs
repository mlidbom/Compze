using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Compze.Threading;

[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "RunOnce instances are long-lived singletons/statics that outlive any meaningful disposal scope")]
public class RunOnce
{
   int _ran;
   readonly ManualResetEventSlim _completed = new();
   Exception? _exception;

   public bool IsFirstCall() => _ran == 0 && Interlocked.CompareExchange(ref _ran, 1, 0) == 0;

   public void RunIfFirstCall(Action action)
   {
      if(IsFirstCall())
      {
         try
         {
            action();
         }
         catch(Exception ex)
         {
            _exception = ex;
            throw;
         }
         finally
         {
            _completed.Set();
         }
      } else
      {
         _completed.Wait();
         if(_exception is not null) throw new AggregateException(_exception);
      }
   }
}
