namespace Compze.Tessaging.Endpoints;

///<summary>Thrown when <see cref="IEndpoint.AwaitReadinessAsync"/> exhausts its patience: the endpoint could still not reach<br/>
/// a handler for every awaited type. The message names each type still unavailable and what the endpoint's peer memory<br/>
/// remembers about it — a remembered peer serving it that is down, or nothing ever met serving it: almost certainly a<br/>
/// deployment or configuration error. Public because it reaches the awaiting application code — typically a startup<br/>
/// sequence deciding to abort — which must be able to catch it.</summary>
public class EndpointNotReadyWithinPatienceException(string message) : Exception(message);
