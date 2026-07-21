using Compze.Abstractions.Public;
using Compze.TypeIdentifiers;

namespace Compze.Teventive.TeventStore.Abstractions._internal.SqlLayer.Abstractions;

class CreationTeventRow(TaggregateId taggregateId, TypeId typeId)
{
   public TaggregateId TaggregateId { get; } = taggregateId;
   public TypeId TypeId { get; } = typeId;
}