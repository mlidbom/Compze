using Compze.Tessaging.Endpoints;
using Compze.Internals.Sql.Common;
using Compze.Internals.Sql.MicrosoftSql;
using Compze.Internals.Sql.MicrosoftSql.Private;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Transport.SqlLayer;
using Compze.TypeIdentifiers.Interning;
using DispatchingTable = Compze.Tessaging.Transport.SqlLayer.ITessagingSqlLayer.OutboxTessageDispatchingTableSchemaStrings;
using TessageTable = Compze.Tessaging.Transport.SqlLayer.ITessagingSqlLayer.OutboxTessagesDatabaseSchemaStrings;

namespace Compze.Tessaging.MicrosoftSql;

partial class MsSqlOutboxSqlLayer(IMsSqlConnectionPool connectionFactory, MsSqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner, EndpointTableSet tables) : ITessagingSqlLayer.IOutboxSqlLayer
{
   readonly IMsSqlConnectionPool _connectionFactory = connectionFactory;
   readonly MsSqlSqlLayerSchemaManager _schemaManager = schemaManager;
   readonly ITypeIdInterner _typeIdInterner = typeIdInterner;
   readonly EndpointTableSet _tables = tables;

   public async Task SaveTessageAsync(ITessagingSqlLayer.OutboxTessageWithReceivers tessageWithReceivers)
   {
      // Intern before opening a connection: interning may hit the database, and nesting a second connection
      // inside a held one deadlocks the pool.
      var internedTypeId = _typeIdInterner.GetOrInternId(tessageWithReceivers.TypeId);
      await _connectionFactory.UseCommandAsync(
         async command =>
         {
            command
              .SetCommandText(
                  $"""

                   INSERT {_tables.OutboxTessages}
                               ({TessageTable.TessageId},  {TessageTable.TypeId}, {TessageTable.SerializedTessage})
                       VALUES (@{TessageTable.TessageId}, @{TessageTable.TypeId}, @{TessageTable.SerializedTessage})

                   """)
              .AddParameter(TessageTable.TessageId, tessageWithReceivers.TessageId.Value)
              .AddParameter(TessageTable.TypeId, internedTypeId)
               //performance: Like with the tevent store, keep all framework properties out of the JSON and put it into separate columns instead. For tevents. Reuse a pre-serialized instance from the persisting to the tevent store.
              .AddNVarcharMaxParameter(TessageTable.SerializedTessage, tessageWithReceivers.SerializedTessage)
              .AddParameter(DispatchingTable.IsReceived, 0);

            tessageWithReceivers.ReceiverEndpointIds.ForEach(
               (endpointId, index)
                  => command.AppendCommandText($"""

                                                INSERT {_tables.OutboxTessageDispatching}
                                                            ({DispatchingTable.TessageId},  {DispatchingTable.EndpointId},          {DispatchingTable.IsReceived})
                                                    VALUES (@{DispatchingTable.TessageId}, @{DispatchingTable.EndpointId}_{index}, @{DispatchingTable.IsReceived})

                                                """).AddParameter($"{DispatchingTable.EndpointId}_{index}", endpointId.Value));

            return await command.ExecuteNonQueryAsync().caf();
         }).caf();
   }

   public async Task<ITessagingSqlLayer.MarkAsReceivedResult> MarkAsReceivedAsync(TessageId tessageId, EndpointId endpointId)
   {
      var affectedRows = await _connectionFactory.UseCommandAsync(
         async command => await command
                   .SetCommandText(
                       $"""

                        UPDATE {_tables.OutboxTessageDispatching}
                            SET {DispatchingTable.IsReceived} = 1
                        WHERE {DispatchingTable.TessageId} = @{DispatchingTable.TessageId}
                            AND {DispatchingTable.EndpointId} = @{DispatchingTable.EndpointId}
                            AND {DispatchingTable.IsReceived} = 0

                        """)
                   .AddParameter(DispatchingTable.TessageId, tessageId.Value)
                   .AddParameter(DispatchingTable.EndpointId, endpointId.Value)
                   .ExecuteNonQueryAsync().caf()).caf();

      return affectedRows == 1
                ? ITessagingSqlLayer.MarkAsReceivedResult.Initial
                : ITessagingSqlLayer.MarkAsReceivedResult.WasAlreadyMarked;
   }

   public async Task RecordDeliveryFailureAsync(TessageId tessageId, EndpointId endpointId, string failureReason)
   {
      await _connectionFactory.UseCommandAsync(
         async command => await command
                   .SetCommandText(
                       $"""

                        UPDATE {_tables.OutboxTessageDispatching}
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
                   .ExecuteNonQueryAsync().caf()).caf();
   }

   public async Task<IReadOnlyList<ITessagingSqlLayer.UndeliveredTessage>> GetUndeliveredTessagesForEndpointAsync(EndpointId endpointId)
   {
      // The TypeId column holds an interned int. Resolve it to the canonical type string AFTER the reader has
      // closed — resolving during the read could open a second connection on a cache miss while the reader is held.
      var raw = await _connectionFactory.UseCommandAsync(
         async command =>
         {
            var rows = new List<(TessageId TessageId, int TypeId, string Body)>();

            command
               .SetCommandText(
                   $"""

                    SELECT m.{TessageTable.TessageId},
                           m.{TessageTable.TypeId},
                           m.{TessageTable.SerializedTessage}
                    FROM {_tables.OutboxTessages} m
                    INNER JOIN {_tables.OutboxTessageDispatching} d ON m.{TessageTable.TessageId} = d.{DispatchingTable.TessageId}
                    WHERE d.{DispatchingTable.IsReceived} = 0
                      AND d.{DispatchingTable.IsStranded} = 0 -- A stranded tessage waits for explicit resolution on the decommission surface, never for delivery.
                      AND d.{DispatchingTable.EndpointId} = @endpointId
                    ORDER BY m.{TessageTable.GeneratedId} -- Send order: recovery re-establishes in-order delivery, oldest undelivered first.

                    """)
               .AddParameter("endpointId", endpointId.Value);

            var reader = await command.ExecuteReaderAsync().caf();
            await using var _ = reader.caf();
            while(await reader.ReadAsync().caf())
            {
               rows.Add((new TessageId(reader.GetGuid(0)),
                         reader.GetInt32(1),
                         reader.GetString(2)));
            }

            return rows;
         }).caf();

      return [..raw.Select(row => new ITessagingSqlLayer.UndeliveredTessage(
                              tessageId: row.TessageId,
                              typeId: _typeIdInterner.GetTypeId(row.TypeId),
                              serializedTessage: row.Body))];
   }

   public async Task DiscardUndeliveredTessagesAsync(EndpointId endpointId, IReadOnlyList<TessageId> tessageIds)
   {
      if(tessageIds.Count == 0) return;
      await _connectionFactory.UseCommandAsync(
         async command =>
         {
            command
              .SetCommandText(
                  $"""

                   DELETE FROM {_tables.OutboxTessageDispatching}
                   WHERE {DispatchingTable.EndpointId} = @{DispatchingTable.EndpointId}
                     AND {DispatchingTable.TessageId} IN ( {TessageIdParameterList(tessageIds.Count)} )

                   """)
              .AddParameter(DispatchingTable.EndpointId, endpointId.Value);
            tessageIds.ForEach((tessageId, index) => command.AddParameter($"{DispatchingTable.TessageId}_{index}", tessageId.Value));
            return await command.ExecuteNonQueryAsync().caf();
         }).caf();
   }

   public async Task StrandUndeliveredTessagesAsync(EndpointId endpointId, IReadOnlyList<TessageId> tessageIds)
   {
      if(tessageIds.Count == 0) return;
      await _connectionFactory.UseCommandAsync(
         async command =>
         {
            command
              .SetCommandText(
                  $"""

                   UPDATE {_tables.OutboxTessageDispatching}
                       SET {DispatchingTable.IsStranded} = 1
                   WHERE {DispatchingTable.EndpointId} = @{DispatchingTable.EndpointId}
                     AND {DispatchingTable.TessageId} IN ( {TessageIdParameterList(tessageIds.Count)} )

                   """)
              .AddParameter(DispatchingTable.EndpointId, endpointId.Value);
            tessageIds.ForEach((tessageId, index) => command.AddParameter($"{DispatchingTable.TessageId}_{index}", tessageId.Value));
            return await command.ExecuteNonQueryAsync().caf();
         }).caf();
   }

   public async Task<IReadOnlyList<ITessagingSqlLayer.DiscardedTessage>> DiscardAllTessagesOwedToAsync(EndpointId endpointId)
   {
      // The TypeId column holds an interned int. Resolve it to the canonical type string AFTER the reader has
      // closed — resolving during the read could open a second connection on a cache miss while the reader is held.
      var owed = await _connectionFactory.UseCommandAsync(
         async command =>
         {
            var rows = new List<(TessageId TessageId, int TypeId, bool WasStranded)>();

            command
               .SetCommandText(
                   $"""

                    SELECT d.{DispatchingTable.TessageId},
                           m.{TessageTable.TypeId},
                           d.{DispatchingTable.IsStranded}
                    FROM {_tables.OutboxTessageDispatching} d
                    INNER JOIN {_tables.OutboxTessages} m ON m.{TessageTable.TessageId} = d.{DispatchingTable.TessageId}
                    WHERE d.{DispatchingTable.IsReceived} = 0
                      AND d.{DispatchingTable.EndpointId} = @endpointId

                    """)
               .AddParameter("endpointId", endpointId.Value);

            var reader = await command.ExecuteReaderAsync().caf();
            await using var _ = reader.caf();
            while(await reader.ReadAsync().caf())
            {
               rows.Add((new TessageId(reader.GetGuid(0)),
                         reader.GetInt32(1),
                         reader.GetBoolean(2)));
            }

            return rows;
         }).caf();

      await DiscardUndeliveredTessagesAsync(endpointId, [..owed.Select(row => row.TessageId)]).caf();

      return [..owed.Select(row => new ITessagingSqlLayer.DiscardedTessage(
                              tessageId: row.TessageId,
                              typeId: _typeIdInterner.GetTypeId(row.TypeId),
                              wasStranded: row.WasStranded))];
   }

   static string TessageIdParameterList(int tessageIdCount) =>
      string.Join(", ", Enumerable.Range(0, tessageIdCount).Select(index => $"@{DispatchingTable.TessageId}_{index}"));

   public async Task InitAsync() => await _schemaManager.EnsureSchemaInitializedAsync().caf();
}
