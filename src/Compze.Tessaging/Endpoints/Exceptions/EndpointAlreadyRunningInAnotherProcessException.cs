namespace Compze.Tessaging.Endpoints.Exceptions;

///<summary>An endpoint runs in exactly one process at a time, and another live process already holds this endpoint's process<br/>
/// lock in the domain database's endpoint catalog. Thrown at startup — two processes claiming the same endpoint is a<br/>
/// misconfiguration, typically a double deployment. The refusal is immediate: the lock is exclusivity a live holder holds<br/>
/// (a database session, or an OS lock for machine-local engines), so it being held is itself proof of a live holder —<br/>
/// a crashed process's lock is released by the infrastructure, letting a restart claim the endpoint with no waiting.<br/>
/// The message names the endpoint and the holding process.</summary>
public class EndpointAlreadyRunningInAnotherProcessException : Exception
{
   internal EndpointAlreadyRunningInAnotherProcessException(string message) : base(message) {}
}
