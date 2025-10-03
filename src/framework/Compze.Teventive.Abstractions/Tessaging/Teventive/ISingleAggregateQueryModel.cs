using System;
using Compze.DDD;
using Compze.DDD.Abstractions;

namespace Compze.Tessaging.Teventive;

public interface ISingleAggregateQueryModel : IHasPersistentIdentity<Guid>
{
   void SetId(Guid id);
}