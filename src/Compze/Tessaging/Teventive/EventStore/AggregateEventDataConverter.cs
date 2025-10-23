using Compze.Tessaging.Teventive.EventStore.Abstractions;
using Compze.Tessaging.Teventive.EventStore.SqlLayer.Abstractions;

namespace Compze.Tessaging.Teventive.EventStore;

static class AggregateEventDataConverter
{
   internal static AggregateEventData ToAggregateEventData(this IAggregateEvent @this) =>
      new AggregateEventData(@this.MessageId, @this.AggregateVersion, @this.AggregateId, @this.UtcTimeStamp);
}
