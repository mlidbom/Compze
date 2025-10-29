using System;
using Compze.Core.Public;

namespace Compze.Core.Tessaging.Teventive.TeventStore.QueryModels;

public interface ISingleTaggregateQueryModel : IHasPersistentIdentity<Guid>
{
   void SetId(Guid id);
}