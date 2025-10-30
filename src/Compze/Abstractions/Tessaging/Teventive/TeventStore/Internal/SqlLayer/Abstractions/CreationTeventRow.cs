using Compze.Core.Public;
using System;

namespace Compze.Core.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;

public class CreationTeventRow(TaggregateId taggregateId, Guid typeId)
{
   public TaggregateId TaggregateId { get; } = taggregateId;
   public Guid TypeId { get; } = typeId;
}