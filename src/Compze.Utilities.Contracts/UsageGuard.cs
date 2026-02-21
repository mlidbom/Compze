namespace Compze.Utilities.Contracts;

///<summary>Base class that takes care of most of the complexity of implementing <see cref="IUsageGuard"/></summary>
public abstract class UsageGuard : IUsageGuard
{
   ///<summary>Implementations throw an exception if the context has changed.</summary>
   public void EnsureAccessValid() => InternalAssertUsageAllowed();

   ///<summary>Implemented by inheritors to do the actual check for any context changes. Implementations throw an exception if the context has changed.</summary>
   protected abstract void InternalAssertUsageAllowed();
}
