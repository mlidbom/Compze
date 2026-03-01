using System;
using Compze.Contracts;

namespace Compze.Threading.Utilities;

///<summary>Simple utility class that calls the supplied action when the instance is disposed. Calls for each dispose call, there is no check.</summary>
class LockDisposer : IDisposable
{
   readonly Action _action;

   ///<summary>Constructs an instance that will call <param name="action"> when disposed.</param></summary>
   public LockDisposer(Action action)
   {
      Contract.Argument.NotNull(action);
      _action = action;
   }

   ///<summary>Invokes the action passed to the constructor.</summary>
   public void Dispose() => _action();
}
