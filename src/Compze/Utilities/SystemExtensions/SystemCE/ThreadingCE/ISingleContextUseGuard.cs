using System;

namespace Compze.Utilities.SystemCE.ThreadingCE;

///<summary>Implementations ensure that a component is only used within the allowed context. Such as a single thread, single http request etc.</summary>
interface ISingleContextUseGuard
{
   ///<summary>Implementations throw an exception if the context has changed.</summary>
   void AssertNoContextChangeOccurred(object guarded);
}

class SingleContextUseGuard<TWrapped> where TWrapped : notnull
{
   readonly TWrapped _wrapped;
   readonly ISingleContextUseGuard _guard;

   public SingleContextUseGuard(TWrapped wrapped, ISingleContextUseGuard guard)
   {
      _wrapped = wrapped;
      _guard = guard;
   }

   public TWrapped Wrapped
   {
      get
      {
         _guard.AssertNoContextChangeOccurred(_wrapped);
         return _wrapped;
      }
   }
}
