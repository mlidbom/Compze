using Compze.Tessaging.Endpoints;

namespace Compze.Tessaging._private.EndpointCatalog;

///<summary>The database session holding an endpoint's process lock died under a live holder — the domain database is<br/>
/// unreachable from this process. The lock is thereby released, so another process may legitimately claim the endpoint<br/>
/// while this process still runs it. Reported through the background-exception machinery so the host fails loud; the<br/>
/// inner exception is the ping failure that evidenced the session's death.</summary>
class EndpointProcessLockSessionLostException : Exception
{
   internal EndpointProcessLockSessionLostException(EndpointConfiguration endpoint, Exception sessionDeath)
      : base($"The database session holding the process lock for endpoint '{endpoint.Name}' ({endpoint.Id}) died: the domain database is unreachable from this process, and the lock is thereby released, so another process may now claim the endpoint while this process still runs it. Shut this process down or restore connectivity.",
             sessionDeath) {}
}
