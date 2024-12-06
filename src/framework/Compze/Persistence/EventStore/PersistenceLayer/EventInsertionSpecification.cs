namespace Compze.Persistence.EventStore.PersistenceLayer;

class EventInsertionSpecification(IAggregateEvent @event, int insertedVersion, int effectiveVersion)
{
   public EventInsertionSpecification(IAggregateEvent @event) : this(@event, @event.AggregateVersion, @event.AggregateVersion) {}

   internal IAggregateEvent Event { get; } = @event;
   internal int InsertedVersion { get; } = insertedVersion;
   internal int EffectiveVersion { get; } = effectiveVersion;
}