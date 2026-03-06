using Compze.Abstractions.Public;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Internal.SqlLayer;
using Compze.Abstractions.Tessaging.Public;

namespace Compze.Tessaging.Implementation.Outbox;

partial class Outbox
{
   public interface ITessageStorage
   {
      void SaveTessage(IExactlyOnceTessage tessage, params EndpointId[] receiverEndpointIds);
      void MarkAsReceived(TessageId tessageId, EndpointId receiverId);
      void RecordDeliveryFailure(TessageId tessageId, EndpointId receiverId, Exception? exception);
      IReadOnlyList<IServiceBusSqlLayer.UndeliveredTessage> GetUndeliveredTessagesForEndpoint(EndpointId endpointId);
      Task StartAsync();
   }
}