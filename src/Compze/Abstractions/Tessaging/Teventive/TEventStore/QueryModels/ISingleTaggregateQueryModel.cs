using System;
using Compze.Core.Public;

namespace Compze.Core.Tessaging.Teventive.TEventStore.QueryModels;

public interface ISingleTaggregateQueryModel : IHasPersistentIdentity<Guid>
{
   void SetId(Guid id);
}