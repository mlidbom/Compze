﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Compze.Contracts;
using Compze.Refactoring.Naming;
using Compze.Serialization;
using Compze.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Messaging.Buses.Implementation;

partial class Outbox
{
   internal class MessageStorage(IServiceBusPersistenceLayer.IOutboxPersistenceLayer persistenceLayer, ITypeMapper typeMapper, IRemotableMessageSerializer serializer) : Outbox.IMessageStorage
   {
      readonly IServiceBusPersistenceLayer.IOutboxPersistenceLayer _persistenceLayer = persistenceLayer;
      readonly ITypeMapper _typeMapper = typeMapper;
      readonly IRemotableMessageSerializer _serializer = serializer;

      public void SaveMessage(IExactlyOnceMessage message, params EndpointId[] receiverEndpointIds)
      {
         var outboxMessageWithReceivers = new IServiceBusPersistenceLayer.OutboxMessageWithReceivers(_serializer.SerializeMessage(message),
                                                                                                     _typeMapper.GetId(message.GetType()).GuidValue,
                                                                                                     message.MessageId,
                                                                                                     receiverEndpointIds.Select(it => it.GuidValue));

         _persistenceLayer.SaveMessage(outboxMessageWithReceivers);
      }

      public void MarkAsReceived(Guid messageId, EndpointId receiverId)
      {
         var endpointIdGuidValue = receiverId.GuidValue;
         var affectedRows = _persistenceLayer.MarkAsReceived(messageId, endpointIdGuidValue);
         Assert.Result.Is(affectedRows == 1);
      }

      public async Task StartAsync() => await _persistenceLayer.InitAsync().CaF();
   }
}