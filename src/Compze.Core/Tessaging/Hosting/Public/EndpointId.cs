using Compze.Core.Public;
using System;
using JetBrains.Annotations;

namespace Compze.Core.Tessaging.Hosting.Public;

public class EndpointId(Guid id) : TentityId(id)
{
   [UsedImplicitly] public EndpointId() : this(Guid.NewGuid()) {}
}
