using System;

namespace Compze.Tessaging.Teventive.EventStore.PersistenceLayer.Abstractions;

public class VersionSpecification(Guid eventId, int version)
{
   public Guid EventId { get; } = eventId;
   public int EffectiveVersion { get; } = version;
}