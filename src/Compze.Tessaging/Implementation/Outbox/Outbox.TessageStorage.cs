using Compze.Abstractions.Public;
using Compze.TypeIdentifiers;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
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

      public void SaveTessage(ITessage tessage, TessageId dedupId, params EndpointId[] receiverEndpointIds)
      {
         var outboxTessageWithReceivers = new ITessagingSqlLayer.OutboxTessageWithReceivers(_serializer.SerializeTessage(tessage),
                                                                                             _typeMap.GetId(tessage.GetType()),
                                                                                             dedupId,
                                                                                             receiverEndpointIds);

         _sqlLayer.SaveTessage(outboxTessageWithReceivers);
      }

      public void MarkAsReceived(TessageId tessageId, EndpointId receiverId)
      {
         var result = _sqlLayer.MarkAsReceived(tessageId, receiverId);

         if(result == ITessagingSqlLayer.MarkAsReceivedResult.WasAlreadyMarked)
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

      public IReadOnlyList<ITessagingSqlLayer.UndeliveredTessage> GetUndeliveredTessagesForEndpoint(EndpointId endpointId) =>
         _sqlLayer.GetUndeliveredTessagesForEndpoint(endpointId);

      public void DiscardUndeliveredTessages(EndpointId endpointId, IReadOnlyList<TessageId> tessageIds) =>
         _sqlLayer.DiscardUndeliveredTessages(endpointId, tessageIds);

      public void StrandUndeliveredTessages(EndpointId endpointId, IReadOnlyList<TessageId> tessageIds) =>
         _sqlLayer.StrandUndeliveredTessages(endpointId, tessageIds);

      public IReadOnlyList<ITessagingSqlLayer.DiscardedTessage> DiscardAllTessagesOwedTo(EndpointId endpointId) =>
         _sqlLayer.DiscardAllTessagesOwedTo(endpointId);

      public async Task StartAsync() => await _sqlLayer.InitAsync().caf();
   }
}
