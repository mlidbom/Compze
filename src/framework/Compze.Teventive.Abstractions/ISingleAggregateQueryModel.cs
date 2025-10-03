using System;
using Compze.DDD;
using Compze.DDD.Abstractions;

namespace Compze.Teventive.Abstractions;

public interface ISingleAggregateQueryModel : IHasPersistentIdentity<Guid>
{
   void SetId(Guid id);
}