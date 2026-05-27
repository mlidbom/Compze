using Compze.Abstractions.Public;
using Compze.TypeIdentifiers;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Internal.SqlLayer;
using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Tessaging.Implementation.Outbox;

partial class Outbox
{
   internal class TessageStorage : ITessageStorage
   {
      // ReSharper disable once MemberHidesStaticFromOuterClass
      internal static void RegisterWith(IComponentRegistrar registrar)
         => registrar.Register(Singleton.For<ITessageStorage>()
                                        .CreatedBy((IServiceBusSqlLayer.IOutboxSqlLayer sqlLayer, ITypeMap typeMap, IRemotableTessageSerializer serializer)
                                                      => new TessageStorage(sqlLayer, typeMap, serializer)));

      readonly IServiceBusSqlLayer.IOutboxSqlLayer _sqlLayer;
      readonly ITypeMap _typeMap;
      readonly IRemotableTessageSerializer _serializer;

      TessageStorage(IServiceBusSqlLayer.IOutboxSqlLayer sqlLayer, ITypeMap typeMap, IRemotableTessageSerializer serializer)
      {
         _sqlLayer = sqlLayer;
         _typeMap = typeMap;
         _serializer = serializer;
      }

      public void SaveTessage(IExactlyOnceTessage tessage, params EndpointId[] receiverEndpointIds)
      {
         var outboxTessageWithReceivers = new IServiceBusSqlLayer.OutboxTessageWithReceivers(_serializer.SerializeTessage(tessage),
                                                                                             _typeMap.GetId(tessage.GetType()).LeafStorageGuid(),
                                                                                             tessage.Id,
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

      public IReadOnlyList<IServiceBusSqlLayer.UndeliveredTessage> GetUndeliveredTessagesForEndpoint(EndpointId endpointId) =>
         _sqlLayer.GetUndeliveredTessagesForEndpoint(endpointId);

      public async Task StartAsync() => await _sqlLayer.InitAsync().caf();
   }
}
