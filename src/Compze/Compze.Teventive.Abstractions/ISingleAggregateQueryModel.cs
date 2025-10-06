using System;
using Compze.Abstractions;

namespace Compze.Teventive.Abstractions;

public interface ISingleAggregateQueryModel : IHasPersistentIdentity<Guid>
{
   void SetId(Guid id);
}