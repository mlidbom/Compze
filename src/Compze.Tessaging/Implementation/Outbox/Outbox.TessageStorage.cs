using Compze.Abstractions.Public;
using Compze.TypeIdentifiers;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Transport.SqlLayer;

namespace Compze.Tessaging.Implementation.Outbox;

// ReSharper disable once ClassCannotBeInstantiated rider is plain confused
partial class Outbox
{
   internal class TessageStorage : ITessageStorage
   {
      // ReSharper disable once MemberHidesStaticFromOuterClass
      internal static void RegisterWith(IComponentRegistrar registrar)
         => registrar.Register(Singleton.For<ITessageStorage>()
                                        .CreatedBy((IServiceBusSqlLayer.IOutboxSqlLayer sqlLayer, ITypeMap typeMap, ITessagingSerializer serializer)
                                                      => new TessageStorage(sqlLayer, typeMap, serializer)));

      readonly IServiceBusSqlLayer.IOutboxSqlLayer _sqlLayer;
      readonly ITypeMap _typeMap;
      readonly ITessagingSerializer _serializer;

      TessageStorage(IServiceBusSqlLayer.IOutboxSqlLayer sqlLayer, ITypeMap typeMap, ITessagingSerializer serializer)
      {
         _sqlLayer = sqlLayer;
         _typeMap = typeMap;
         _serializer = serializer;
      }

      public void SaveTessage(ITessage tessage, TessageId dedupId, params EndpointId[] receiverEndpointIds)
      {
         var outboxTessageWithReceivers = new IServiceBusSqlLayer.OutboxTessageWithReceivers(_serializer.SerializeTessage(tessage),
                                                                                             _typeMap.GetId(tessage.GetType()),
                                                                                             dedupId,
                                                                                             receiverEndpointIds);

         _sqlLayer.SaveTessage(outboxTessageWithReceivers);
      }

      public void MarkAsReceived(TessageId tessageId, EndpointId receiverId)
      {
         var result = _sqlLayer.MarkAsReceived(tessageId, receiverId);

         if(result == IServiceBusSqlLayer.MarkAsReceivedResult.WasAlreadyMarked)
         {
            this.Log().Info($"Tessage {tessageId} to endpoint {receiverId.Value} was already marked as received.");
         }
      }

      public void RecordDeliveryFailure(TessageId tessageId, EndpointId receiverId, Exception? exception)
      {
         var failureReason = exception != null
                                ? $"{exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}"
                                : "Unknown failure";

         _sqlLayer.RecordDeliveryFailure(tessageId, receiverId, failureReason);
      }

      public IReadOnlyList<IServiceBusSqlLayer.UndeliveredTessage> GetUndeliveredTessagesForEndpoint(EndpointId endpointId, IReadOnlySet<string> advertisedHandledTessageTypes)
      {
         //The advertisement partitions the way routing partitions it: tevent subscriptions are the ITevent wrapper types,
         //everything else is a tommand type - the types whose unbound rows this endpoint's connection should carry.
         var handledTommandTypes = advertisedHandledTessageTypes.Select(_typeMap.GetId)
                                                                .Where(typeId => !typeId.Type.Is<ITevent>())
                                                                .ToList();
         return _sqlLayer.GetUndeliveredTessagesForEndpoint(endpointId, handledTommandTypes);
      }

      public async Task StartAsync() => await _sqlLayer.InitAsync().caf();
   }
}
