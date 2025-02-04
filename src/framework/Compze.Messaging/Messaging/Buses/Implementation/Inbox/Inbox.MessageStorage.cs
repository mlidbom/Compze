﻿using System;
using System.Threading.Tasks;
using Compze.Contracts;
using Compze.SystemCE.ReflectionCE;

namespace Compze.Messaging.Buses.Implementation;

class InboxMessageStorage(IServiceBusPersistenceLayer.IInboxPersistenceLayer persistenceLayer) : Inbox.IMessageStorage
{
   readonly IServiceBusPersistenceLayer.IInboxPersistenceLayer _persistenceLayer = persistenceLayer;

   public void SaveIncomingMessage(TransportMessage.InComing message)
      => _persistenceLayer.SaveMessage(message.MessageId, message.MessageTypeId.GuidValue, message.Body);

   public void MarkAsSucceeded(TransportMessage.InComing message)
      => _persistenceLayer.MarkAsSucceeded(message.MessageId);

   public void RecordException(TransportMessage.InComing message, Exception exception)
   {
      var affectedRows = _persistenceLayer.RecordException(message.MessageId,
                                                           exception.StackTrace ?? string.Empty,
                                                           exception.Message,
                                                           exception.GetType().GetFullNameCompilable());

      Assert.Result.Is(affectedRows == 1);
   }

   public void MarkAsFailed(TransportMessage.InComing message)
   {
      var affectedRows = _persistenceLayer.MarkAsFailed(message.MessageId);
      Assert.Result.Is(affectedRows == 1);
   }

   public Task StartAsync() => _persistenceLayer.InitAsync();
}