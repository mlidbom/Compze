using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Compze.Sql.Common;
using Compze.Sql.PostgreSql;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Utilities.SystemCE.LinqCE;
using NpgsqlTypes;
using TessageTable = Compze.Tessaging.Hosting.Implementation.IServiceBusSqlLayer.OutboxTessagesDatabaseSchemaStrings;
using DispatchingTable = Compze.Tessaging.Hosting.Implementation.IServiceBusSqlLayer.OutboxTessageDispatchingTableSchemaStrings;

namespace Compze.Tessaging.Sql.PostgreSql;

partial class PgSqlOutboxSqlLayer(IPgSqlConnectionPool connectionFactory) : IServiceBusSqlLayer.IOutboxSqlLayer
{
   readonly IPgSqlConnectionPool _connectionFactory = connectionFactory;

   public void SaveTessage(IServiceBusSqlLayer.OutboxTessageWithReceivers tessageWithReceivers)
   {
      _connectionFactory.UseCommand(
         command =>
         {
            command
              .SetCommandText(
                  $"""

                   INSERT INTO {TessageTable.TableName} 
                               ({TessageTable.TessageId},  {TessageTable.TypeIdGuidValue}, {TessageTable.SerializedTessage}) 
                       VALUES (@{TessageTable.TessageId}, @{TessageTable.TypeIdGuidValue}, @{TessageTable.SerializedTessage});

                   """)
              .AddParameter(TessageTable.TessageId, tessageWithReceivers.TessageId)
              .AddParameter(TessageTable.TypeIdGuidValue, tessageWithReceivers.TypeIdGuidValue)
               //performance: Like with the event store, keep all framework properties out of the JSON and put it into separate columns instead. For events. Reuse a pre-serialized instance from the persisting to the event store.
              .AddMediumTextParameter(TessageTable.SerializedTessage, tessageWithReceivers.SerializedTessage)
              .AddParameter(DispatchingTable.IsReceived, NpgsqlDbType.Boolean, false);

            tessageWithReceivers.ReceiverEndpointIds.ForEach(
               (endpointId, index)
                  => command.AppendCommandText($"""

                                                INSERT INTO {DispatchingTable.TableName} 
                                                            ({DispatchingTable.TessageId},  {DispatchingTable.EndpointId},          {DispatchingTable.IsReceived}) 
                                                    VALUES (@{DispatchingTable.TessageId}, @{DispatchingTable.EndpointId}_{index}, @{DispatchingTable.IsReceived});

                                                """).AddParameter($"{DispatchingTable.EndpointId}_{index}", endpointId));

            command
              .PrepareStatement() //Even though the count above will differ it will probably not have too many variations for preparing the statement to be quite beneficial.
              .ExecuteNonQuery();
         });
   }

   public IServiceBusSqlLayer.MarkAsReceivedResult MarkAsReceived(Guid tessageId, Guid endpointId)
   {
      var affectedRows = _connectionFactory.UseCommand(
         command => command
                   .SetCommandText(
                       $"""

                        UPDATE {DispatchingTable.TableName} 
                            SET {DispatchingTable.IsReceived} = true
                        WHERE {DispatchingTable.TessageId} = @{DispatchingTable.TessageId}
                            AND {DispatchingTable.EndpointId} = @{DispatchingTable.EndpointId}
                            AND {DispatchingTable.IsReceived} = false;

                        """)
                   .AddParameter(DispatchingTable.TessageId, tessageId)
                   .AddParameter(DispatchingTable.EndpointId, endpointId)
                   .PrepareStatement()
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
                            AND {DispatchingTable.EndpointId} = @{DispatchingTable.EndpointId};

                        """)
                   .AddParameter(DispatchingTable.TessageId, tessageId)
                   .AddParameter(DispatchingTable.EndpointId, endpointId)
                   .AddTimestampWithTimeZone(DispatchingTable.LastAttemptTime, DateTime.UtcNow)
                   .AddMediumTextParameter(DispatchingTable.FailureReason, failureReason)
                   .PrepareStatement()
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
                    WHERE d.{DispatchingTable.IsReceived} = false
                      AND (d.{DispatchingTable.LastAttemptTime} IS NULL 
                           OR d.{DispatchingTable.LastAttemptTime} < @cutoffTime)
                    ORDER BY d.{DispatchingTable.RetryCount}, d.{DispatchingTable.LastAttemptTime} NULLS FIRST;

                    """)
               .AddTimestampWithTimeZone("cutoffTime", cutoffTime)
               .PrepareStatement();
            
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