using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Transport.Internal;
using Compze.Tessaging.Hosting.Implementation;

namespace Compze.Tessaging.Implementation.Outbox;

partial class Outbox
{
   public interface ITessageStorage
   {
      void SaveTessage(IExactlyOnceTessage tessage, params EndpointId[] receiverEndpointIds);
      void MarkAsReceived(Guid tessageId, EndpointId receiverId);
      void RecordDeliveryFailure(Guid tessageId, EndpointId receiverId, Exception? exception);
      IReadOnlyList<IServiceBusSqlLayer.UndeliveredTessage> GetUndeliveredTessages(TimeSpan olderThan);
      Task StartAsync();
   }
}