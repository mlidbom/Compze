using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Sql.Common.TeventStore.Abstractions;

namespace Compze.Tessaging.Teventive.TeventStore;

static class AggregateTeventDataConverter
{
   internal static AggregateTeventData ToAggregateTeventData(this IAggregateTevent @this) =>
      new AggregateTeventData(@this.TessageId, @this.AggregateVersion, @this.AggregateId, @this.UtcTimeStamp);
}
