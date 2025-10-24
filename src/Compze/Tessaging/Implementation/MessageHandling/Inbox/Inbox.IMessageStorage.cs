using System;
using System.Threading.Tasks;
using Compze.Tessaging.Hosting.Implementation;

namespace Compze.Tessaging.Implementation.MessageHandling;

partial class Inbox
{
   public interface IMessageStorage
   {
      IServiceBusSqlLayer.SaveMessageResult SaveIncomingMessage(TransportMessage.InComing message);
      void MarkAsSucceeded(TransportMessage.InComing message);
      void RecordException(TransportMessage.InComing message, Exception exception );
      void MarkAsFailed(TransportMessage.InComing message);
      Task StartAsync();
   }
}