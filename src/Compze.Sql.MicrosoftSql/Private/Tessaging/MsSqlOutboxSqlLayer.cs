using Compze.Core.Public;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Internal.SqlLayer;
using Compze.Sql.Common;
using Compze.Utilities.SystemCE.LinqCE;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Threading.TasksCE;
using DispatchingTable = Compze.Core.Tessaging.Internal.SqlLayer.IServiceBusSqlLayer.OutboxTessageDispatchingTableSchemaStrings;
using TessageTable = Compze.Core.Tessaging.Internal.SqlLayer.IServiceBusSqlLayer.OutboxTessagesDatabaseSchemaStrings;

namespace Compze.Sql.MicrosoftSql.Private.Tessaging;

internal partial class MsSqlOutboxSqlLayer(IMsSqlConnectionPool connectionFactory, MsSqlSqlLayerSchemaManager schemaManager) : IServiceBusSqlLayer.IOutboxSqlLayer
{
   readonly IMsSqlConnectionPool _connectionFactory = connectionFactory;
   readonly MsSqlSqlLayerSchemaManager _schemaManager = schemaManager;

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
              .AddParameter(TessageTable.TessageId, tessageWithReceivers.TessageId.Value)
              .AddParameter(TessageTable.TypeIdGuidValue, tessageWithReceivers.TypeId.Value)
               //performance: Like with the tevent store, keep all framework properties out of the JSON and put it into separate columns instead. For tevents. Reuse a pre-serialized instance from the persisting to the tevent store.
              .AddNVarcharMaxParameter(TessageTable.SerializedTessage, tessageWithReceivers.SerializedTessage)
              .AddParameter(DispatchingTable.IsReceived, 0);

            tessageWithReceivers.ReceiverEndpointIds.ForEach(
               (endpointId, index)
                  => command.AppendCommandText($"""

                                                INSERT {DispatchingTable.TableName} 
                                                            ({DispatchingTable.TessageId},  {DispatchingTable.EndpointId},          {DispatchingTable.IsReceived}) 
                                                    VALUES (@{DispatchingTable.TessageId}, @{DispatchingTable.EndpointId}_{index}, @{DispatchingTable.IsReceived})

                                                """).AddParameter($"{DispatchingTable.EndpointId}_{index}", endpointId.Value));

            command.ExecuteNonQuery();
         });
   }

   public IServiceBusSqlLayer.MarkAsReceivedResult MarkAsReceived(TessageId tessageId, EndpointId endpointId)
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
                   .AddParameter(DispatchingTable.TessageId, tessageId.Value)
                   .AddParameter(DispatchingTable.EndpointId, endpointId.Value)
                   .ExecuteNonQuery());

      return affectedRows == 1
                ? IServiceBusSqlLayer.MarkAsReceivedResult.Initial
                : IServiceBusSqlLayer.MarkAsReceivedResult.WasAlreadyMarked;
   }

   public void RecordDeliveryFailure(TessageId tessageId, EndpointId endpointId, string failureReason)
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
                   .AddParameter(DispatchingTable.TessageId, tessageId.Value)
                   .AddParameter(DispatchingTable.EndpointId, endpointId.Value)
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
                  tessageId: new TessageId(reader.GetGuid(0)),
                  typeId: new TypeId(reader.GetGuid(1)),
                  serializedTessage: reader.GetString(2),
                  targetEndpointId: new EndpointId(reader.GetGuid(3)),
                  retryCount: reader.GetInt32(4),
                  lastAttemptTime: reader.IsDBNull(5) ? null : reader.GetDateTime(5)));
            }

            return tessages;
         });
   }

   public IReadOnlyList<IServiceBusSqlLayer.UndeliveredTessage> GetUndeliveredTessagesForEndpoint(EndpointId endpointId)
   {
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
                      AND d.{DispatchingTable.EndpointId} = @endpointId
                    ORDER BY d.{DispatchingTable.RetryCount}, d.{DispatchingTable.LastAttemptTime}

                    """)
               .AddParameter("endpointId", endpointId.Value);

            using var reader = command.ExecuteReader();
            while(reader.Read())
            {
               tessages.Add(new IServiceBusSqlLayer.UndeliveredTessage(
                  tessageId: new TessageId(reader.GetGuid(0)),
                  typeId: new TypeId(reader.GetGuid(1)),
                  serializedTessage: reader.GetString(2),
                  targetEndpointId: new EndpointId(reader.GetGuid(3)),
                  retryCount: reader.GetInt32(4),
                  lastAttemptTime: reader.IsDBNull(5) ? null : reader.GetDateTime(5)));
            }

            return tessages;
         });
   }

   public async Task InitAsync() => await _schemaManager.EnsureSchemaInitializedAsync().caf();
}
