using System;

namespace Compze.EventStore.PersistenceLayer.Abstractions;

public class VersionSpecification(Guid eventId, int version)
{
   public Guid EventId { get; } = eventId;
   public int EffectiveVersion { get; } = version;
}