using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Transport.Internal;
using Compze.Sql.Common.Tessaging;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Logging;
using Compze.Utilities.Threading.TasksCE;

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
                                                                                             _typeMapper.GetId(tessage.GetType()).GuidValue,
                                                                                             tessage.TessageId,
                                                                                             receiverEndpointIds.Select(it => it.GuidValue));

         _sqlLayer.SaveTessage(outboxTessageWithReceivers);
      }

      public void MarkAsReceived(Guid tessageId, EndpointId receiverId)
      {
         var endpointIdGuidValue = receiverId.GuidValue;
         var result = _sqlLayer.MarkAsReceived(tessageId, endpointIdGuidValue);

         if(result == IServiceBusSqlLayer.MarkAsReceivedResult.WasAlreadyMarked)
         {
            this.Log().Info($"Tessage {tessageId} to endpoint {receiverId.GuidValue} was already marked as received.");
         }
      }

      public void RecordDeliveryFailure(Guid tessageId, EndpointId receiverId, Exception? exception)
      {
         var failureReason = exception != null
                                ? $"{exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}"
                                : "Unknown failure";

         _sqlLayer.RecordDeliveryFailure(tessageId, receiverId.GuidValue, failureReason);
      }

      public IReadOnlyList<IServiceBusSqlLayer.UndeliveredTessage> GetUndeliveredTessages(TimeSpan olderThan) =>
         _sqlLayer.GetUndeliveredTessages(olderThan);

      public async Task StartAsync() => await _sqlLayer.InitAsync().caf();
   }
}
