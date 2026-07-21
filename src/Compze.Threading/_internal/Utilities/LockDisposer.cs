using Compze.Contracts;

namespace Compze.Threading._internal.Utilities;

///<summary>Simple utility class that calls the supplied action when the instance is disposed. Implements all lock token interfaces so it can be returned as any of them.</summary>
class LockDisposer : ILock, IReadLock, IUpdateLock
{
   readonly Action _action;

   ///<summary>Constructs an instance that will call <param name="action"> when disposed.</param></summary>
   public LockDisposer(Action action)
   {
      Argument.NotNull(action);
      _action = action;
   }

   ///<summary>Invokes the action passed to the constructor.</summary>
   public void Dispose() => _action();
}
