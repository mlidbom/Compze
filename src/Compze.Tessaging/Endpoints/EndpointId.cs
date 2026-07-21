using Compze.Abstractions;

namespace Compze.Tessaging.Endpoints;

///<summary>Durably identifies an endpoint across processes and restarts — unlike <see cref="EndpointConfiguration.Name"/>, which is for humans and may change. Used for routing and as the endpoint's identity in persisted data, so production endpoints must pass a fixed Guid rather than use the generating constructor.</summary>
public class EndpointId(Guid id) : TentityId(id)
{
   public EndpointId() : this(Guid.NewGuid()) {}
}
