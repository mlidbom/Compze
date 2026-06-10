using Compze.Abstractions.Public;

namespace Compze.Abstractions.Hosting.Public;

public class EndpointId(Guid id) : TentityId(id)
{
   public EndpointId() : this(Guid.NewGuid()) {}
}
