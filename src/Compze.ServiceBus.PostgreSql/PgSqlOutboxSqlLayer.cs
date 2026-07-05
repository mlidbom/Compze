using Compze.Abstractions.Public;
using Compze.Abstractions.Hosting.Public;
using Compze.Internals.Sql.Common;
using Compze.Internals.Sql.Common.Abstractions;
using Compze.Internals.Sql.PostgreSql;
using Compze.Internals.Sql.PostgreSql.Private;
using Compze.Internals.SystemCE.LinqCE;
using NpgsqlTypes;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.ServiceBus.Transport.SqlLayer;
using Compze.TypeIdentifiers.Interning;
using DispatchingTable = Compze.ServiceBus.Transport.SqlLayer.IServiceBusSqlLayer.OutboxTessageDispatchingTableSchemaStrings;
using TessageTable = Compze.ServiceBus.Transport.SqlLayer.IServiceBusSqlLayer.OutboxTessagesDatabaseSchemaStrings;

namespace Compze.ServiceBus.PostgreSql;

partial class PgSqlOutboxSqlLayer(IPgSqlConnectionPool connectionFactory, PgSqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) : IServiceBusSqlLayer.IOutboxSqlLayer
{
   readonly IPgSqlConnectionPool _connectionFactory = connectionFactory;
   readonly PgSqlSqlLayerSchemaManager _schemaManager = schemaManager;
   readonly ITypeIdInterner _typeIdInterner = typeIdInterner;

   public void SaveTessage(IServiceBusSqlLayer.OutboxTessageWithReceivers tessageWithReceivers)
   {
      // Intern before opening a connection: interning may hit the database, and nesting a second connection
      // inside a held one deadlocks the pool.
      var internedTypeId = _typeIdInterner.GetOrInternId(tessageWithReceivers.TypeId);
      _connectionFactory.UseCommand(
         command =>
         {
            command
              .SetCommandText(
                  $"""

                   INSERT INTO {TessageTable.TableName} 
                               ({TessageTable.TessageId},  {TessageTable.TypeId}, {TessageTable.SerializedTessage}) 
                       VALUES (@{TessageTable.TessageId}, @{TessageTable.TypeId}, @{TessageTable.SerializedTessage});

                   """)
              .AddParameter(TessageTable.TessageId, tessageWithReceivers.TessageId.Value)
              .AddParameter(TessageTable.TypeId, internedTypeId)
               //performance: Like with the tevent store, keep all framework properties out of the JSON and put it into separate columns instead. For tevents. Reuse a pre-serialized instance from the persisting to the tevent store.
              .AddMediumTextParameter(TessageTable.SerializedTessage, tessageWithReceivers.SerializedTessage)
              .AddParameter(DispatchingTable.IsReceived, NpgsqlDbType.Boolean, false);

            tessageWithReceivers.ReceiverEndpointIds.ForEach(
               (endpointId, index)
                  => command.AppendCommandText($"""

                                                INSERT INTO {DispatchingTable.TableName} 
                                                            ({DispatchingTable.TessageId},  {DispatchingTable.EndpointId},          {DispatchingTable.IsReceived}) 
                                                    VALUES (@{DispatchingTable.TessageId}, @{DispatchingTable.EndpointId}_{index}, @{DispatchingTable.IsReceived});

                                                """).AddParameter($"{DispatchingTable.EndpointId}_{index}", endpointId.Value));

            command
              .PrepareStatement() //Even though the count above will differ it will probably not have too many variations for preparing the statement to be quite beneficial.
              .ExecuteNonQuery();
         });
   }

   public IServiceBusSqlLayer.MarkAsReceivedResult MarkAsReceived(TessageId tessageId, EndpointId endpointId)
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
                   .AddParameter(DispatchingTable.TessageId, tessageId.Value)
                   .AddParameter(DispatchingTable.EndpointId, endpointId.Value)
                   .PrepareStatement()
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
                            AND {DispatchingTable.EndpointId} = @{DispatchingTable.EndpointId};

                        """)
                   .AddParameter(DispatchingTable.TessageId, tessageId.Value)
                   .AddParameter(DispatchingTable.EndpointId, endpointId.Value)
                   .AddTimestampWithTimeZone(DispatchingTable.LastAttemptTime, DateTime.UtcNow)
                   .AddMediumTextParameter(DispatchingTable.FailureReason, failureReason)
                   .PrepareStatement()
                   .ExecuteNonQuery());
   }

   public IReadOnlyList<IServiceBusSqlLayer.UndeliveredTessage> GetUndeliveredTessagesForEndpoint(EndpointId endpointId)
   {
      // The TypeId column holds an interned int. Resolve it to the canonical type string AFTER the reader has
      // closed — resolving during the read could open a second connection on a cache miss while the reader is held.
      var raw = _connectionFactory.UseCommand(
         command =>
         {
            var rows = new List<(TessageId TessageId, int TypeId, string Body, EndpointId Endpoint, int RetryCount, DateTime? LastAttempt)>();

            command
               .SetCommandText(
                   $"""

                    SELECT m.{TessageTable.TessageId},
                           m.{TessageTable.TypeId},
                           m.{TessageTable.SerializedTessage},
                           d.{DispatchingTable.EndpointId},
                           d.{DispatchingTable.RetryCount},
                           d.{DispatchingTable.LastAttemptTime}
                    FROM {TessageTable.TableName} m
                    INNER JOIN {DispatchingTable.TableName} d ON m.{TessageTable.TessageId} = d.{DispatchingTable.TessageId}
                    WHERE d.{DispatchingTable.IsReceived} = false
                      AND d.{DispatchingTable.EndpointId} = @endpointId
                    ORDER BY d.{DispatchingTable.RetryCount}, d.{DispatchingTable.LastAttemptTime} NULLS FIRST;

                    """)
               .AddParameter("endpointId", endpointId.Value)
               .PrepareStatement();

            using var reader = command.ExecuteReader();
            while(reader.Read())
            {
               rows.Add((new TessageId(reader.GetGuid(0)),
                         reader.GetInt32(1),
                         reader.GetString(2),
                         new EndpointId(reader.GetGuid(3)),
                         reader.GetInt32(4),
                         reader.IsDBNull(5) ? null : reader.GetDateTime(5)));
            }

            return rows;
         });

      return raw.Select(row => new IServiceBusSqlLayer.UndeliveredTessage(
                           tessageId: row.TessageId,
                           typeId: _typeIdInterner.GetTypeId(row.TypeId),
                           serializedTessage: row.Body,
                           targetEndpointId: row.Endpoint,
                           retryCount: row.RetryCount,
                           lastAttemptTime: row.LastAttempt)).ToList();
   }

   public async Task InitAsync() => await _schemaManager.EnsureSchemaInitializedAsync().caf();
}
