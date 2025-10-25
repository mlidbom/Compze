using Compze.Abstractions.Tessaging.Teventive.EventStore.Public;
using Compze.Sql.Common.EventStore.Abstractions;

namespace Compze.Tessaging.Teventive.EventStore;

static class AggregateEventDataConverter
{
   internal static AggregateEventData ToAggregateEventData(this IAggregateEvent @this) =>
      new AggregateEventData(@this.MessageId, @this.AggregateVersion, @this.AggregateId, @this.UtcTimeStamp);
}
