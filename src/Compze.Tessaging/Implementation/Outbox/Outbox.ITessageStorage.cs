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

      ///<summary>The endpoint's recovery backlog: every tessage bound to it and not yet received, in send order.</summary>
      IReadOnlyList<IServiceBusSqlLayer.UndeliveredTessage> GetUndeliveredTessagesForEndpoint(EndpointId endpointId);

      Task StartAsync();
   }
}
