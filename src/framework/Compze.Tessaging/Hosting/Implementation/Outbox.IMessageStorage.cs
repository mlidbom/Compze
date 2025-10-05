using System;
using System.Threading.Tasks;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Hosting.Abstractions;

namespace Compze.Tessaging.Hosting.Implementation;

partial class Outbox
{
   public interface IMessageStorage
   {
      void SaveMessage(IExactlyOnceMessage message, params EndpointId[] receiverEndpointIds);
      void MarkAsReceived(Guid messageId, EndpointId receiverId);
      Task StartAsync();
   }
}