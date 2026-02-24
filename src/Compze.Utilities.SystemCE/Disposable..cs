using System;
using System.Threading;
using Compze.Contracts;

namespace Compze.Utilities.SystemCE;

///<summary>Simple utility class that calls the supplied action when the instance is disposed. Gets rid of the need to create a ton of small classes to do cleanup.</summary>
public class Disposable : IDisposable
{
   int _ran = 0;
   readonly Action _onDispose;

   ///<summary>Constructs an instance that will call <param name="onDispose"> when disposed.</param></summary>
   public Disposable(Action onDispose)
   {
      Assert.Argument.NotNull(onDispose);
      _onDispose = onDispose;
   }

   ///<summary>Invokes the action passed to the constructor.</summary>
   public void Dispose()
   {
      if(_ran == 0 && Interlocked.CompareExchange(ref _ran, 1, 0) == 0)
      {
         _onDispose();
      }
   }

   public static readonly IDisposable NullOp = new Disposable(ActionCE.NullOp);
}
