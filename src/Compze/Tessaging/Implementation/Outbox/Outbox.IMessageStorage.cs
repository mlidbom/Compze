using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Hosting.Abstractions.Transport;
using Compze.Tessaging.Hosting.Implementation;

namespace Compze.Tessaging.Implementation.Outbox;

partial class Outbox
{
   public interface IMessageStorage
   {
      void SaveMessage(IExactlyOnceMessage message, params EndpointId[] receiverEndpointIds);
      void MarkAsReceived(Guid messageId, EndpointId receiverId);
      void RecordDeliveryFailure(Guid messageId, EndpointId receiverId, Exception? exception);
      IReadOnlyList<IServiceBusSqlLayer.UndeliveredMessage> GetUndeliveredMessages(TimeSpan olderThan);
      Task StartAsync();
   }
}