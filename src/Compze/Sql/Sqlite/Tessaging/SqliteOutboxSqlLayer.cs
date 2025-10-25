using System;
using System.Collections.Generic;
using Compze.Sql.Common;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Utilities.SystemCE.LinqCE;
using System.Globalization;
using System.Threading.Tasks;
using Compze.Sql.Sqlite;
using TessageTable = Compze.Tessaging.Hosting.Implementation.IServiceBusSqlLayer.OutboxTessagesDatabaseSchemaStrings;
using DispatchingTable = Compze.Tessaging.Hosting.Implementation.IServiceBusSqlLayer.OutboxTessageDispatchingTableSchemaStrings;

namespace Compze.Tessaging.Sql.Sqlite;

partial class SqliteOutboxSqlLayer(ISqliteConnectionPool connectionFactory) : IServiceBusSqlLayer.IOutboxSqlLayer
{
   readonly ISqliteConnectionPool _connectionFactory = connectionFactory;

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
              .AddVarcharParameter(TessageTable.TessageId, 36, tessageWithReceivers.TessageId.ToString())
              .AddVarcharParameter(TessageTable.TypeIdGuidValue, 36, tessageWithReceivers.TypeIdGuidValue.ToString())
              .AddMediumTextParameter(TessageTable.SerializedTessage, tessageWithReceivers.SerializedTessage)
              .AddParameter(DispatchingTable.IsReceived, 0);

            tessageWithReceivers.ReceiverEndpointIds.ForEach(
               (endpointId, index)
                  => command.AppendCommandText($"""

                                                INSERT INTO {DispatchingTable.TableName} 
                                                            ({DispatchingTable.TessageId},  {DispatchingTable.EndpointId},          {DispatchingTable.IsReceived}) 
                                                    VALUES (@{DispatchingTable.TessageId}, @{DispatchingTable.EndpointId}_{index}, @{DispatchingTable.IsReceived});

                                                """).AddVarcharParameter($"{DispatchingTable.EndpointId}_{index}", 36, endpointId.ToString()));

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
                   .AddVarcharParameter(DispatchingTable.TessageId, 36, tessageId.ToString())
                   .AddVarcharParameter(DispatchingTable.EndpointId, 36, endpointId.ToString())
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
                   .AddVarcharParameter(DispatchingTable.TessageId, 36, tessageId.ToString())
                   .AddVarcharParameter(DispatchingTable.EndpointId, 36, endpointId.ToString())
                   .AddVarcharParameter(DispatchingTable.LastAttemptTime, 50, DateTime.UtcNow.ToString("O"))
                   .AddMediumTextParameter(DispatchingTable.FailureReason, failureReason)
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
               .AddVarcharParameter("cutoffTime", 50, cutoffTime.ToString("O"));
            
            using var reader = command.ExecuteReader();
            while(reader.Read())
            {
               tessages.Add(new IServiceBusSqlLayer.UndeliveredTessage(
                  tessageId: Guid.Parse(reader.GetString(0)),
                  typeIdGuid: Guid.Parse(reader.GetString(1)),
                  serializedTessage: reader.GetString(2),
                  targetEndpointId: Guid.Parse(reader.GetString(3)),
                  retryCount: reader.GetInt32(4),
                  lastAttemptTime: reader.IsDBNull(5) ? null : DateTime.Parse(reader.GetString(5), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)));
            }
            
            return tessages;
         });
   }

   public Task InitAsync() => SchemaManager.EnsureTablesExistAsync(_connectionFactory);
}
