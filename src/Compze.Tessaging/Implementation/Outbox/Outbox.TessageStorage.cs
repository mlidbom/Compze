using Compze.Core.Public;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Serialization.Internal;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Internal.SqlLayer;
using Compze.Core.Tessaging.Public;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Logging;
using Compze.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Tessaging.Implementation.Outbox;

partial class Outbox
{
   internal class TessageStorage : ITessageStorage
   {
      // ReSharper disable once MemberHidesStaticFromOuterClass
      internal static void RegisterWith(IComponentRegistrar registrar)
         => registrar.Register(Singleton.For<ITessageStorage>()
                                        .CreatedBy((IServiceBusSqlLayer.IOutboxSqlLayer sqlLayer, ITypeMapper typeMapper, IRemotableTessageSerializer serializer)
                                                      => new TessageStorage(sqlLayer, typeMapper, serializer)));

      readonly IServiceBusSqlLayer.IOutboxSqlLayer _sqlLayer;
      readonly ITypeMapper _typeMapper;
      readonly IRemotableTessageSerializer _serializer;

      TessageStorage(IServiceBusSqlLayer.IOutboxSqlLayer sqlLayer, ITypeMapper typeMapper, IRemotableTessageSerializer serializer)
      {
         _sqlLayer = sqlLayer;
         _typeMapper = typeMapper;
         _serializer = serializer;
      }

      public void SaveTessage(IExactlyOnceTessage tessage, params EndpointId[] receiverEndpointIds)
      {
         var outboxTessageWithReceivers = new IServiceBusSqlLayer.OutboxTessageWithReceivers(_serializer.SerializeTessage(tessage),
                                                                                             _typeMapper.GetId(tessage.GetType()),
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

      public IReadOnlyList<IServiceBusSqlLayer.UndeliveredTessage> GetUndeliveredTessages(TimeSpan olderThan) =>
         _sqlLayer.GetUndeliveredTessages(olderThan);

      public IReadOnlyList<IServiceBusSqlLayer.UndeliveredTessage> GetUndeliveredTessagesForEndpoint(EndpointId endpointId) =>
         _sqlLayer.GetUndeliveredTessagesForEndpoint(endpointId);

      public async Task StartAsync() => await _sqlLayer.InitAsync().caf();
   }
}
