namespace Compze.Utilities.Contracts.UsageGuards;

///<summary>Implementations ensure that any preconditions for using an object are fulfilled, and throws an exception if it could not successfully do so.</summary>
public interface IUsageGuard
{
   ///<summary>Implementations throw an exception if the guard could not guarantee the preconditions.</summary>
   void EnsureAccessValid();
}

public class UsageGuard<TWrapped> where TWrapped : notnull
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
         _guard.EnsureAccessValid();
         return _wrapped;
      }
   }
}
