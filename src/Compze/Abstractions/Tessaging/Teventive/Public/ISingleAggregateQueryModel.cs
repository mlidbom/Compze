using System;
using Compze.Abstractions;
using Compze.Abstractions.Public;

namespace Compze.Tessaging.Teventive.Abstractions;

public interface ISingleAggregateQueryModel : IHasPersistentIdentity<Guid>
{
   void SetId(Guid id);
}