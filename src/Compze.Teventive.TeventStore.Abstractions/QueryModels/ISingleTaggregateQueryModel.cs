using Compze.Abstractions.Public;

namespace Compze.Teventive.TeventStore.Abstractions.QueryModels;

public interface ISingleTaggregateQueryModel : IEntity<Guid>
{
   void SetId(TaggregateId id);
}