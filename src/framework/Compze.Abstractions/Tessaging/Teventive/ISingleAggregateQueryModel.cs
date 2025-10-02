using System;
using Compze.DDD;

namespace Compze.Tessaging.Teventive;

public interface ISingleAggregateQueryModel : IHasPersistentIdentity<Guid>
{
   void SetId(Guid id);
}