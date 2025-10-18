using System;
using System.Threading.Tasks;
using Compze.Utilities.Contracts;
using Compze.Utilities.SystemCE.ReflectionCE;

namespace Compze.Tessaging.Hosting.Implementation;

class InboxMessageStorage(IServiceBusSqlLayer.IInboxSqlLayer sqlLayer) : Inbox.IMessageStorage
{
   readonly IServiceBusSqlLayer.IInboxSqlLayer _sqlLayer = sqlLayer;

   public IServiceBusSqlLayer.SaveMessageResult SaveIncomingMessage(TransportMessage.InComing message)
      => _sqlLayer.SaveMessage(message.MessageId, message.MessageTypeId.GuidValue, message.Body);

   public void MarkAsSucceeded(TransportMessage.InComing message)
      => _sqlLayer.MarkAsSucceeded(message.MessageId);

   public void RecordException(TransportMessage.InComing message, Exception exception)
   {
      var affectedRows = _sqlLayer.RecordException(message.MessageId,
                                                           exception.StackTrace ?? string.Empty,
                                                           exception.Message,
                                                           exception.GetType().GetFullNameCompilable());

      Assert.Result.Is(affectedRows == 1);
   }

   public void MarkAsFailed(TransportMessage.InComing message)
   {
      var affectedRows = _sqlLayer.MarkAsFailed(message.MessageId);
      Assert.Result.Is(affectedRows == 1);
   }

   public Task StartAsync() => _sqlLayer.InitAsync();
}