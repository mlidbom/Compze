using Compze.Abstractions;
using Compze.TypeIdentifiers;

namespace Compze.Teventive.TeventStore.Abstractions._internal.SqlLayer.Abstractions;

///<summary>A taggregate's id and the type of one tevent of its history, such as its creation tevent or its last.</summary>
class TaggregateTeventTypeRow(TaggregateId taggregateId, TypeId typeId)
{
   public TaggregateId TaggregateId { get; } = taggregateId;
   public TypeId TypeId { get; } = typeId;
}