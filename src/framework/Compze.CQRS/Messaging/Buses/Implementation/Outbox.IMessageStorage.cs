using System;
using System.Threading.Tasks;

namespace Compze.Messaging.Buses.Implementation;

partial class Outbox
{
   public interface IMessageStorage
   {
      void SaveMessage(IExactlyOnceMessage message, params EndpointId[] receiverEndpointIds);
      void MarkAsReceived(Guid messageId, EndpointId receiverId);
      Task StartAsync();
   }
}