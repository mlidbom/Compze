using Compze.Core.Public;
using System;

namespace Compze.Core.Tessaging.Hosting.Public;

public class EndpointId(Guid id) : TentityId(id)
{
   public EndpointId() : this(Guid.NewGuid()) {}
}
