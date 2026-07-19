using Compze.TypeIdentifiers;
using Compze.Tessaging.Endpoints;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Abstractions.TessageTypes;
using Compze.Tessaging.Serialization;
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
                                        .CreatedBy((ITessagingSqlLayer.IOutboxSqlLayer sqlLayer, ITypeMap typeMap, ITessagingSerializer serializer)
                                                      => new TessageStorage(sqlLayer, typeMap, serializer)));

      readonly ITessagingSqlLayer.IOutboxSqlLayer _sqlLayer;
      readonly ITypeMap _typeMap;
      readonly ITessagingSerializer _serializer;

      TessageStorage(ITessagingSqlLayer.IOutboxSqlLayer sqlLayer, ITypeMap typeMap, ITessagingSerializer serializer)
      {
         _sqlLayer = sqlLayer;
         _typeMap = typeMap;
         _serializer = serializer;
      }

      public async Task SaveTessageAsync(ITessage tessage, TessageId dedupId, params EndpointId[] receiverEndpointIds)
      {
         var outboxTessageWithReceivers = new ITessagingSqlLayer.OutboxTessageWithReceivers(_serializer.SerializeTessage(tessage),
                                                                                             _typeMap.GetId(tessage.GetType()),
                                                                                             dedupId,
                                                                                             receiverEndpointIds);

         await _sqlLayer.SaveTessageAsync(outboxTessageWithReceivers).caf();
      }

      public async Task MarkAsReceivedAsync(TessageId tessageId, EndpointId receiverId)
      {
         var result = await _sqlLayer.MarkAsReceivedAsync(tessageId, receiverId).caf();

         if(result == ITessagingSqlLayer.MarkAsReceivedResult.WasAlreadyMarked)
         {
            this.Log().Info($"Tessage {tessageId} to endpoint {receiverId.Value} was already marked as received.");
         }
      }

      public async Task RecordDeliveryFailureAsync(TessageId tessageId, EndpointId receiverId, Exception? exception)
      {
         var failureReason = exception != null
                                ? $"{exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}"
                                : "Unknown failure";

         await _sqlLayer.RecordDeliveryFailureAsync(tessageId, receiverId, failureReason).caf();
      }

      public async Task<IReadOnlyList<ITessagingSqlLayer.UndeliveredTessage>> GetUndeliveredTessagesForEndpointAsync(EndpointId endpointId) =>
         await _sqlLayer.GetUndeliveredTessagesForEndpointAsync(endpointId).caf();

      public async Task DiscardUndeliveredTessagesAsync(EndpointId endpointId, IReadOnlyList<TessageId> tessageIds) =>
         await _sqlLayer.DiscardUndeliveredTessagesAsync(endpointId, tessageIds).caf();

      public async Task StrandUndeliveredTessagesAsync(EndpointId endpointId, IReadOnlyList<TessageId> tessageIds) =>
         await _sqlLayer.StrandUndeliveredTessagesAsync(endpointId, tessageIds).caf();

      public async Task<IReadOnlyList<ITessagingSqlLayer.DiscardedTessage>> DiscardAllTessagesOwedToAsync(EndpointId endpointId) =>
         await _sqlLayer.DiscardAllTessagesOwedToAsync(endpointId).caf();

      public async Task StartAsync() => await _sqlLayer.InitAsync().caf();
   }
}
