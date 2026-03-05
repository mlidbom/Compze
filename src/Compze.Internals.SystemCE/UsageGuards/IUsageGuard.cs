namespace Compze.Internals.SystemCE.UsageGuards;

///<summary>Implementations ensure that any preconditions for using an object are fulfilled, and throws an exception if it could not successfully do so.</summary>
public interface IUsageGuard
{
   ///<summary>Implementations throw an exception if the guard could not guarantee the preconditions.</summary>
   void EnsureAccessValid();
}

public class UsageGuard<TWrapped>(TWrapped wrapped, IUsageGuard guard)
   where TWrapped : notnull
{
   readonly TWrapped _wrapped = wrapped;
   readonly IUsageGuard _guard = guard;

   public TWrapped Wrapped
   {
      get
      {
         _guard.EnsureAccessValid();
         return _wrapped;
      }
   }
}
