using Compze.Abstractions.Public;

namespace Compze.Core.Tessaging.Teventive.TeventStore.QueryModels;

public interface ISingleTaggregateQueryModel : IEntity<Guid>
{
   void SetId(TaggregateId id);
}