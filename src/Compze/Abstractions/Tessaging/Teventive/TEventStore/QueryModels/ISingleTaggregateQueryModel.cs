using System;
using Compze.Abstractions.Public;

namespace Compze.Abstractions.Tessaging.Teventive.TEventStore.QueryModels;

public interface ISingleTaggregateQueryModel : IHasPersistentIdentity<Guid>
{
   void SetId(Guid id);
}