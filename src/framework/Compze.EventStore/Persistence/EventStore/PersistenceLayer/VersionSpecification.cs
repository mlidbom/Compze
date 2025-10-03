using System;

namespace Compze.Persistence.EventStore.PersistenceLayer;

public class VersionSpecification(Guid eventId, int version)
{
   public Guid EventId { get; } = eventId;
   public int EffectiveVersion { get; } = version;
}