using System.Text.Json.Serialization;
using Compze.Abstractions;

namespace Compze.Tessaging.Endpoints;

///<summary>Durably identifies an endpoint across processes and restarts — unlike <see cref="EndpointConfiguration.Name"/>, which is for humans and may change. Used for routing and as the endpoint's identity in persisted data, so production endpoints must pass a fixed Guid rather than use <see cref="New"/>.</summary>
[JsonConverter(typeof(EntityIdJsonConverter))]
public class EndpointId(Guid value) : TentityId(value)
{
   ///<summary>A new <see cref="EndpointId"/>, of a new random <see cref="Guid"/>: for an endpoint that lives only as long as its process, such as in a test.</summary>
   public new static EndpointId New() => new(Guid.NewGuid());
}
