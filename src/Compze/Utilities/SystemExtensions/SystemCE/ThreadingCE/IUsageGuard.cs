using Compze.Utilities.GenericAbstractions.Wrappers;

namespace Compze.Utilities.SystemCE.ThreadingCE;

///<summary>Implementations ensure that a component is only used within the allowed context. Such as a single thread, single http request etc.</summary>
interface IUsageGuard
{
   ///<summary>Implementations throw an exception if the context has changed.</summary>
   void AssertUseValid();
}

class UsageGuard<TWrapped> where TWrapped : notnull
{
   readonly TWrapped _wrapped;
   readonly IUsageGuard _guard;

   public UsageGuard(TWrapped wrapped, IUsageGuard guard)
   {
      _wrapped = wrapped;
      _guard = guard;
   }

   public TWrapped Wrapped
   {
      get
      {
         _guard.AssertUseValid();
         return _wrapped;
      }
   }
}
