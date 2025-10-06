using System;
using Compze.Abstractions;

namespace Compze.Tessaging.Teventive.Abstractions;

public interface ISingleAggregateQueryModel : IHasPersistentIdentity<Guid>
{
   void SetId(Guid id);
}