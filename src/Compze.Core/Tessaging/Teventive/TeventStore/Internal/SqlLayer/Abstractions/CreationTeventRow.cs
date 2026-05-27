using Compze.Abstractions.Public;

namespace Compze.Core.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;

public class CreationTeventRow(TaggregateId taggregateId, string typeId)
{
   public TaggregateId TaggregateId { get; } = taggregateId;
   public string TypeId { get; } = typeId;
}