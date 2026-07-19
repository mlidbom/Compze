namespace Compze.Tessaging.Endpoints.Exceptions;

///<summary>An endpoint runs in exactly one process at a time, and another live process already holds this endpoint's process<br/>
/// lease in the domain database's endpoint catalog. Thrown at startup — two processes claiming the same endpoint is a<br/>
/// misconfiguration, typically a double deployment — after the starting process has waited out one full lease duration to<br/>
/// rule out a dead predecessor whose lease had not yet gone stale. The message names the endpoint and the holding process.</summary>
///<remarks>Also reported through the background-exception machinery when a running endpoint discovers its lease was taken:<br/>
/// its heartbeats went unrefreshed past the lease duration — a long pause such as a debugger, a machine sleep — and a<br/>
/// claimant presumed it dead, so two processes may momentarily be running the endpoint.</remarks>
public class EndpointAlreadyRunningInAnotherProcessException : Exception
{
   internal EndpointAlreadyRunningInAnotherProcessException(string message) : base(message) {}
}
