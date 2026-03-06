using Compze.Abstractions.Tessaging.Public;

namespace Compze.Tessaging.Implementation.TessageHandling;

class DefaultRetryPolicy
{
   const int Tries = 5;
   int _remainingTries;
   // ReSharper disable UnusedParameter.Local parameters are there to enable implementation to take the type of tessage and exception into account when deciding on whether to retry and how long to wait before retrying.
#pragma warning disable IDE0060 //Reviewed OK: parameters are there to enable implementation to take the type of tessage and exception into account when deciding on whether to retry and how long to wait before retrying.
#pragma warning disable CA1801  // Review unused parameters
   public DefaultRetryPolicy(ITessage tessage) => _remainingTries = Tries;
#pragma warning restore CA1801 // Review unused parameters
#pragma warning disable CA1801 // Review unused parameters
   // ReSharper disable once UnusedParameter.Global
   public bool TryAwaitNextRetryTimeForException(Exception exception) => --_remainingTries > 0;
#pragma warning restore CA1801  // Review unused parameters
#pragma warning restore IDE0060 // Remove unused parameter
   // ReSharper restore UnusedParameter.Local
}