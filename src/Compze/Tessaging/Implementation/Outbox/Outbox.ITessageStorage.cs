using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Public;
using Compze.Sql.Common.Tessaging;

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