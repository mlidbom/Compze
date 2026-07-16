using Compze.Abstractions.Public;
using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Tessaging.Transport.SqlLayer;

namespace Compze.Tessaging.Implementation.Outbox;

// ReSharper disable once ClassCannotBeInstantiated rider is plain confused
partial class Outbox
{
   public interface ITessageStorage
   {
      void SaveTessage(ITessage tessage, TessageId dedupId, params EndpointId[] receiverEndpointIds);
      void MarkAsReceived(TessageId tessageId, EndpointId receiverId);
      void RecordDeliveryFailure(TessageId tessageId, EndpointId receiverId, Exception? exception);

      ///<summary>The endpoint's recovery backlog, in send order: everything bound to it plus every unbound tommand whose type<br/>
      /// its advertisement — <paramref name="advertisedHandledTessageTypes"/>, the canonical type strings off the wire —<br/>
      /// handles (route-at-delivery; see <see cref="IServiceBusSqlLayer.IOutboxSqlLayer.GetUndeliveredTessagesForEndpoint"/>).</summary>
      IReadOnlyList<IServiceBusSqlLayer.UndeliveredTessage> GetUndeliveredTessagesForEndpoint(EndpointId endpointId, IReadOnlySet<string> advertisedHandledTessageTypes);

      Task StartAsync();
   }
}
