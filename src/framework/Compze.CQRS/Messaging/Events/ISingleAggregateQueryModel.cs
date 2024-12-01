using System;
using Compze.DDD;

namespace Compze.Messaging.Events;

public interface ISingleAggregateQueryModel : IHasPersistentIdentity<Guid>
{
   void SetId(Guid id);
}