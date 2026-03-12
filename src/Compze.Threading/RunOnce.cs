using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Compze.Threading;

///<summary>Ensures an action runs exactly once, even when called concurrently from multiple threads. Subsequent callers block until the first call completes. If the first call fails, all callers receive the exception.</summary>
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "RunOnce instances are long-lived singletons/statics that outlive any meaningful disposal scope")]
public class RunOnce
{
   int _ran;
   readonly ManualResetEventSlim _completed = new();
   Exception? _exception;

   ///<summary>Returns true if this is the first call, false for all subsequent calls. Thread-safe.</summary>
   public bool IsFirstCall() => _ran == 0 && Interlocked.CompareExchange(ref _ran, 1, 0) == 0;

   ///<summary>Executes <paramref name="action"/> if this is the first call. Subsequent callers block until the first call completes. If the first call threw, subsequent callers receive the exception wrapped in an <see cref="AggregateException"/>.</summary>
   public void RunIfFirstCall([InstantHandle]Action action)
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
