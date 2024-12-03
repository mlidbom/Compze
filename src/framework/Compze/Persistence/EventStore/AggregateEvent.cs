using System;
using Compze.Messaging;

namespace Compze.Persistence.EventStore;

public abstract class AggregateEvent<TBaseEventInterface>(TBaseEventInterface @event) : WrapperEvent<TBaseEventInterface>(@event), IAggregateEvent<TBaseEventInterface>
   where TBaseEventInterface : IAggregateEvent;

public abstract class AggregateEvent() : IAggregateEvent
{
   protected AggregateEvent(Guid aggregateId) : this() => AggregateId = aggregateId;

   /*Refactor: Consider making these fields read-only and then generating accessor for them at runtime. This would make the special nature of changing them more explicit.
    And it would remove the requirement that this class is used. Another class could be used and we would detect that and generate new setters for that class, requiring
   only that it had private setters (including the one generated for a readonly property by the runtime.).
   */
   public Guid MessageId { get; internal set; } = Guid.NewGuid();
   public int AggregateVersion { get; internal set; }

   //Refactor: We should most likely use a custom type for Ids....
   public Guid AggregateId { get; internal set; }
   public DateTime UtcTimeStamp { get; internal set; } = DateTime.UtcNow; //Todo:bug: Should use timesource.
}
