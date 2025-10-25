using Compze.Abstractions.Tessaging.Teventive.EventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Sql.Common.EventStore.Abstractions;

namespace Compze.Tessaging.Teventive.EventStore;

static class AggregateEventDataConverter
{
   internal static AggregateEventData ToAggregateEventData(this IAggregateTevent @this) =>
      new AggregateEventData(@this.TessageId, @this.AggregateVersion, @this.AggregateId, @this.UtcTimeStamp);
}
