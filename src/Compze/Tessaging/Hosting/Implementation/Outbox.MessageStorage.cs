using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Compze.Abstractions.Internal.Refactoring.Naming;
using Compze.Serialization;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Logging;
using Compze.Utilities.Threading.TasksCE;

namespace Compze.Tessaging.Hosting.Implementation;

partial class Outbox
{
   internal class MessageStorage : IMessageStorage
   {
      // ReSharper disable once MemberHidesStaticFromOuterClass
      internal static void RegisterWith(IComponentRegistrar registrar)
         => registrar.Register(Singleton.For<IMessageStorage>()
                                        .CreatedBy((IServiceBusSqlLayer.IOutboxSqlLayer sqlLayer, ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
                                                      => new MessageStorage(sqlLayer, typeMapper, serializer)));

      readonly IServiceBusSqlLayer.IOutboxSqlLayer _sqlLayer;
      readonly ITypeMapper _typeMapper;
      readonly IRemotableMessageSerializer _serializer;

      MessageStorage(IServiceBusSqlLayer.IOutboxSqlLayer sqlLayer, ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
      {
         _sqlLayer = sqlLayer;
         _typeMapper = typeMapper;
         _serializer = serializer;
      }

      public void SaveMessage(IExactlyOnceMessage message, params EndpointId[] receiverEndpointIds)
      {
         var outboxMessageWithReceivers = new IServiceBusSqlLayer.OutboxMessageWithReceivers(_serializer.SerializeMessage(message),
                                                                                             _typeMapper.GetId(message.GetType()).GuidValue,
                                                                                             message.MessageId,
                                                                                             receiverEndpointIds.Select(it => it.GuidValue));

         _sqlLayer.SaveMessage(outboxMessageWithReceivers);
      }

      public void MarkAsReceived(Guid messageId, EndpointId receiverId)
      {
         var endpointIdGuidValue = receiverId.GuidValue;
         var result = _sqlLayer.MarkAsReceived(messageId, endpointIdGuidValue);

         if(result == IServiceBusSqlLayer.MarkAsReceivedResult.WasAlreadyMarked)
         {
            this.Log().Info($"Message {messageId} to endpoint {receiverId.GuidValue} was already marked as received.");
         }
      }

      public void RecordDeliveryFailure(Guid messageId, EndpointId receiverId, Exception? exception)
      {
         var failureReason = exception != null
                                ? $"{exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}"
                                : "Unknown failure";

         _sqlLayer.RecordDeliveryFailure(messageId, receiverId.GuidValue, failureReason);
      }

      public IReadOnlyList<IServiceBusSqlLayer.UndeliveredMessage> GetUndeliveredMessages(TimeSpan olderThan) =>
         _sqlLayer.GetUndeliveredMessages(olderThan);

      public async Task StartAsync() => await _sqlLayer.InitAsync().caf();
   }
}
