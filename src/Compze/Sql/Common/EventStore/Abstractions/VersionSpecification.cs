using System;

namespace Compze.Sql.Common.EventStore.Abstractions;

public class VersionSpecification(Guid eventId, int version)
{
   public Guid EventId { get; } = eventId;
   public int EffectiveVersion { get; } = version;
}