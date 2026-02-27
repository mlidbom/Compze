using Compze.Core.Public;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Internal.SqlLayer;
using Compze.Core.Tessaging.Public;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Compze.Tessaging.Implementation.Outbox;

public partial class Outbox
{
   public interface ITessageStorage
   {
      void SaveTessage(IExactlyOnceTessage tessage, params EndpointId[] receiverEndpointIds);
      void MarkAsReceived(TessageId tessageId, EndpointId receiverId);
      void RecordDeliveryFailure(TessageId tessageId, EndpointId receiverId, Exception? exception);
      IReadOnlyList<IServiceBusSqlLayer.UndeliveredTessage> GetUndeliveredTessages(TimeSpan olderThan);
      IReadOnlyList<IServiceBusSqlLayer.UndeliveredTessage> GetUndeliveredTessagesForEndpoint(EndpointId endpointId);
      Task StartAsync();
   }
}