using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Compze.Sql.Common;
using Compze.Sql.MicrosoftSql;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Utilities.SystemCE.LinqCE;
using TessageTable = Compze.Tessaging.Hosting.Implementation.IServiceBusSqlLayer.OutboxTessagesDatabaseSchemaStrings;
using DispatchingTable = Compze.Tessaging.Hosting.Implementation.IServiceBusSqlLayer.OutboxTessageDispatchingTableSchemaStrings;

namespace Compze.Tessaging.Sql.MicrosoftSql;

partial class MsSqlOutboxSqlLayer(IMsSqlConnectionPool connectionFactory) : IServiceBusSqlLayer.IOutboxSqlLayer
{
   readonly IMsSqlConnectionPool _connectionFactory = connectionFactory;

   public void SaveTessage(IServiceBusSqlLayer.OutboxTessageWithReceivers tessageWithReceivers)
   {
      _connectionFactory.UseCommand(
         command =>
         {
            command
              .SetCommandText(
                  $"""

                   INSERT {TessageTable.TableName} 
                               ({TessageTable.TessageId},  {TessageTable.TypeIdGuidValue}, {TessageTable.SerializedTessage}) 
                       VALUES (@{TessageTable.TessageId}, @{TessageTable.TypeIdGuidValue}, @{TessageTable.SerializedTessage})

                   """)
              .AddParameter(TessageTable.TessageId, tessageWithReceivers.TessageId)
              .AddParameter(TessageTable.TypeIdGuidValue, tessageWithReceivers.TypeIdGuidValue)
               //performance: Like with the event store, keep all framework properties out of the JSON and put it into separate columns instead. For events. Reuse a pre-serialized instance from the persisting to the event store.
              .AddNVarcharMaxParameter(TessageTable.SerializedTessage, tessageWithReceivers.SerializedTessage)
              .AddParameter(DispatchingTable.IsReceived, 0);

            tessageWithReceivers.ReceiverEndpointIds.ForEach(
               (endpointId, index)
                  => command.AppendCommandText($"""

                                                INSERT {DispatchingTable.TableName} 
                                                            ({DispatchingTable.TessageId},  {DispatchingTable.EndpointId},          {DispatchingTable.IsReceived}) 
                                                    VALUES (@{DispatchingTable.TessageId}, @{DispatchingTable.EndpointId}_{index}, @{DispatchingTable.IsReceived})

                                                """).AddParameter($"{DispatchingTable.EndpointId}_{index}", endpointId));

            command.ExecuteNonQuery();
         });
   }

   public IServiceBusSqlLayer.MarkAsReceivedResult MarkAsReceived(Guid tessageId, Guid endpointId)
   {
      var affectedRows = _connectionFactory.UseCommand(
         command => command
                   .SetCommandText(
                       $"""

                        UPDATE {DispatchingTable.TableName} 
                            SET {DispatchingTable.IsReceived} = 1
                        WHERE {DispatchingTable.TessageId} = @{DispatchingTable.TessageId}
                            AND {DispatchingTable.EndpointId} = @{DispatchingTable.EndpointId}
                            AND {DispatchingTable.IsReceived} = 0

                        """)
                   .AddParameter(DispatchingTable.TessageId, tessageId)
                   .AddParameter(DispatchingTable.EndpointId, endpointId)
                   .ExecuteNonQuery());

      return affectedRows == 1
                ? IServiceBusSqlLayer.MarkAsReceivedResult.Initial
                : IServiceBusSqlLayer.MarkAsReceivedResult.WasAlreadyMarked;
   }

   public void RecordDeliveryFailure(Guid tessageId, Guid endpointId, string failureReason)
   {
      _connectionFactory.UseCommand(
         command => command
                   .SetCommandText(
                       $"""

                        UPDATE {DispatchingTable.TableName} 
                            SET {DispatchingTable.RetryCount} = {DispatchingTable.RetryCount} + 1,
                                {DispatchingTable.LastAttemptTime} = @{DispatchingTable.LastAttemptTime},
                                {DispatchingTable.FailureReason} = @{DispatchingTable.FailureReason}
                        WHERE {DispatchingTable.TessageId} = @{DispatchingTable.TessageId}
                            AND {DispatchingTable.EndpointId} = @{DispatchingTable.EndpointId}

                        """)
                   .AddParameter(DispatchingTable.TessageId, tessageId)
                   .AddParameter(DispatchingTable.EndpointId, endpointId)
                   .AddDateTime2Parameter(DispatchingTable.LastAttemptTime, DateTime.UtcNow)
                   .AddNVarcharMaxParameter(DispatchingTable.FailureReason, failureReason)
                   .ExecuteNonQuery());
   }

   public IReadOnlyList<IServiceBusSqlLayer.UndeliveredTessage> GetUndeliveredTessages(TimeSpan olderThan)
   {
      var cutoffTime = DateTime.UtcNow - olderThan;

      return _connectionFactory.UseCommand(
         command =>
         {
            var tessages = new List<IServiceBusSqlLayer.UndeliveredTessage>();

            command
               .SetCommandText(
                   $"""

                    SELECT m.{TessageTable.TessageId}, 
                           m.{TessageTable.TypeIdGuidValue}, 
                           m.{TessageTable.SerializedTessage},
                           d.{DispatchingTable.EndpointId},
                           d.{DispatchingTable.RetryCount},
                           d.{DispatchingTable.LastAttemptTime}
                    FROM {TessageTable.TableName} m
                    INNER JOIN {DispatchingTable.TableName} d ON m.{TessageTable.TessageId} = d.{DispatchingTable.TessageId}
                    WHERE d.{DispatchingTable.IsReceived} = 0
                      AND (d.{DispatchingTable.LastAttemptTime} IS NULL 
                           OR d.{DispatchingTable.LastAttemptTime} < @cutoffTime)
                    ORDER BY d.{DispatchingTable.RetryCount}, d.{DispatchingTable.LastAttemptTime}

                    """)
               .AddDateTime2Parameter("cutoffTime", cutoffTime);

            using var reader = command.ExecuteReader();
            while(reader.Read())
            {
               tessages.Add(new IServiceBusSqlLayer.UndeliveredTessage(
                  tessageId: reader.GetGuid(0),
                  typeIdGuid: reader.GetGuid(1),
                  serializedTessage: reader.GetString(2),
                  targetEndpointId: reader.GetGuid(3),
                  retryCount: reader.GetInt32(4),
                  lastAttemptTime: reader.IsDBNull(5) ? null : reader.GetDateTime(5)));
            }

            return tessages;
         });
   }

   public Task InitAsync() => SchemaManager.EnsureTablesExistAsync(_connectionFactory);
}
