using System;

namespace Compze.Core.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;

public class CreationTeventRow(Guid taggregateId, Guid typeId)
{
   public Guid TaggregateId { get; } = taggregateId;
   public Guid TypeId { get; } = typeId;
}