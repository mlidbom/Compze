using System;
using System.Linq;
using System.Threading.Tasks;
using Compze.Abstractions.Internal.Refactoring.Naming;
using Compze.Tessaging.Abstractions;
using Compze.Serialization;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Utilities.Contracts;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Threading.TasksCE;

namespace Compze.Tessaging.Hosting.Implementation;

partial class Outbox
{
   internal class MessageStorage : Outbox.IMessageStorage
   {
      // ReSharper disable once MemberHidesStaticFromOuterClass
      internal static void RegisterWith(IDependencyRegistrar registrar)
         => registrar.Register(Singleton.For<Outbox.IMessageStorage>()
                                        .CreatedBy((IServiceBusPersistenceLayer.IOutboxPersistenceLayer persistenceLayer, ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
                                                      => new Outbox.MessageStorage(persistenceLayer, typeMapper, serializer)));

      readonly IServiceBusPersistenceLayer.IOutboxPersistenceLayer _persistenceLayer;
      readonly ITypeMapper _typeMapper;
      readonly IRemotableMessageSerializer _serializer;

      MessageStorage(IServiceBusPersistenceLayer.IOutboxPersistenceLayer persistenceLayer, ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
      {
         _persistenceLayer = persistenceLayer;
         _typeMapper = typeMapper;
         _serializer = serializer;
      }

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

      public async Task StartAsync() => await _persistenceLayer.InitAsync().caf();
   }
}
