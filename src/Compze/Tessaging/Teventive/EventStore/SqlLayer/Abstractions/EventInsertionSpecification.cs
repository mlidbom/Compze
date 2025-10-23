namespace Compze.Tessaging.Teventive.EventStore.SqlLayer.Abstractions;

public class EventInsertionSpecification(AggregateEventData @event, int insertedVersion, int effectiveVersion)
{
   public EventInsertionSpecification(AggregateEventData @event) : this(@event, @event.AggregateVersion, @event.AggregateVersion) {}

   internal AggregateEventData Event { get; } = @event;
   internal int InsertedVersion { get; } = insertedVersion;
   internal int EffectiveVersion { get; } = effectiveVersion;
}