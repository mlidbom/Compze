using System;

namespace Compze.Persistence.EventStore.PersistenceLayer;

class VersionSpecification
{
   public VersionSpecification(Guid eventId, int version)
   {
      EventId = eventId;
      EffectiveVersion = version;
   }

   public Guid EventId { get; }
   public int EffectiveVersion { get; }
}