using Compze.Abstractions.Public;
using Compze.TypeIdentifiers;

namespace Compze.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;

public class CreationTeventRow(TaggregateId taggregateId, TypeId typeId)
{
   public TaggregateId TaggregateId { get; } = taggregateId;
   public TypeId TypeId { get; } = typeId;
}