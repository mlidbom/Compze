using Compze.Abstractions.Public;
using Compze.Abstractions.Hosting.Public;
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

partial class MsSqlOutboxSqlLayer(IMsSqlConnectionPool connectionFactory, MsSqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) : ITessagingSqlLayer.IOutboxSqlLayer
{
   readonly IMsSqlConnectionPool _connectionFactory = connectionFactory;
   readonly MsSqlSqlLayerSchemaManager _schemaManager = schemaManager;
   readonly ITypeIdInterner _typeIdInterner = typeIdInterner;

   public void SaveTessage(ITessagingSqlLayer.OutboxTessageWithReceivers tessageWithReceivers)
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

                   INSERT {TessageTable.TableName}
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

                                                INSERT {DispatchingTable.TableName} 
                                                            ({DispatchingTable.TessageId},  {DispatchingTable.EndpointId},          {DispatchingTable.IsReceived}) 
                                                    VALUES (@{DispatchingTable.TessageId}, @{DispatchingTable.EndpointId}_{index}, @{DispatchingTable.IsReceived})

                                                """).AddParameter($"{DispatchingTable.EndpointId}_{index}", endpointId.Value));

            command.ExecuteNonQuery();
         });
   }

   public ITessagingSqlLayer.MarkAsReceivedResult MarkAsReceived(TessageId tessageId, EndpointId endpointId)
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
                ? ITessagingSqlLayer.MarkAsReceivedResult.Initial
                : ITessagingSqlLayer.MarkAsReceivedResult.WasAlreadyMarked;
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

   public IReadOnlyList<ITessagingSqlLayer.UndeliveredTessage> GetUndeliveredTessagesForEndpoint(EndpointId endpointId)
   {
      // The TypeId column holds an interned int. Resolve it to the canonical type string AFTER the reader has
      // closed — resolving during the read could open a second connection on a cache miss while the reader is held.
      var raw = _connectionFactory.UseCommand(
         command =>
         {
            var rows = new List<(TessageId TessageId, int TypeId, string Body)>();

            command
               .SetCommandText(
                   $"""

                    SELECT m.{TessageTable.TessageId},
                           m.{TessageTable.TypeId},
                           m.{TessageTable.SerializedTessage}
                    FROM {TessageTable.TableName} m
                    INNER JOIN {DispatchingTable.TableName} d ON m.{TessageTable.TessageId} = d.{DispatchingTable.TessageId}
                    WHERE d.{DispatchingTable.IsReceived} = 0
                      AND d.{DispatchingTable.IsStranded} = 0 -- A stranded tessage waits for explicit resolution on the decommission surface, never for delivery.
                      AND d.{DispatchingTable.EndpointId} = @endpointId
                    ORDER BY m.{TessageTable.GeneratedId} -- Send order: recovery re-establishes in-order delivery, oldest undelivered first.

                    """)
               .AddParameter("endpointId", endpointId.Value);

            using var reader = command.ExecuteReader();
            while(reader.Read())
            {
               rows.Add((new TessageId(reader.GetGuid(0)),
                         reader.GetInt32(1),
                         reader.GetString(2)));
            }

            return rows;
         });

      return [..raw.Select(row => new ITessagingSqlLayer.UndeliveredTessage(
                              tessageId: row.TessageId,
                              typeId: _typeIdInterner.GetTypeId(row.TypeId),
                              serializedTessage: row.Body))];
   }

   public void DiscardUndeliveredTessages(EndpointId endpointId, IReadOnlyList<TessageId> tessageIds)
   {
      if(tessageIds.Count == 0) return;
      _connectionFactory.UseCommand(
         command =>
         {
            command
              .SetCommandText(
                  $"""

                   DELETE FROM {DispatchingTable.TableName}
                   WHERE {DispatchingTable.EndpointId} = @{DispatchingTable.EndpointId}
                     AND {DispatchingTable.TessageId} IN ( {TessageIdParameterList(tessageIds.Count)} )

                   """)
              .AddParameter(DispatchingTable.EndpointId, endpointId.Value);
            tessageIds.ForEach((tessageId, index) => command.AddParameter($"{DispatchingTable.TessageId}_{index}", tessageId.Value));
            command.ExecuteNonQuery();
         });
   }

   public void StrandUndeliveredTessages(EndpointId endpointId, IReadOnlyList<TessageId> tessageIds)
   {
      if(tessageIds.Count == 0) return;
      _connectionFactory.UseCommand(
         command =>
         {
            command
              .SetCommandText(
                  $"""

                   UPDATE {DispatchingTable.TableName}
                       SET {DispatchingTable.IsStranded} = 1
                   WHERE {DispatchingTable.EndpointId} = @{DispatchingTable.EndpointId}
                     AND {DispatchingTable.TessageId} IN ( {TessageIdParameterList(tessageIds.Count)} )

                   """)
              .AddParameter(DispatchingTable.EndpointId, endpointId.Value);
            tessageIds.ForEach((tessageId, index) => command.AddParameter($"{DispatchingTable.TessageId}_{index}", tessageId.Value));
            command.ExecuteNonQuery();
         });
   }

   public IReadOnlyList<ITessagingSqlLayer.DiscardedTessage> DiscardAllTessagesOwedTo(EndpointId endpointId)
   {
      // The TypeId column holds an interned int. Resolve it to the canonical type string AFTER the reader has
      // closed — resolving during the read could open a second connection on a cache miss while the reader is held.
      var owed = _connectionFactory.UseCommand(
         command =>
         {
            var rows = new List<(TessageId TessageId, int TypeId, bool WasStranded)>();

            command
               .SetCommandText(
                   $"""

                    SELECT d.{DispatchingTable.TessageId},
                           m.{TessageTable.TypeId},
                           d.{DispatchingTable.IsStranded}
                    FROM {DispatchingTable.TableName} d
                    INNER JOIN {TessageTable.TableName} m ON m.{TessageTable.TessageId} = d.{DispatchingTable.TessageId}
                    WHERE d.{DispatchingTable.IsReceived} = 0
                      AND d.{DispatchingTable.EndpointId} = @endpointId

                    """)
               .AddParameter("endpointId", endpointId.Value);

            using var reader = command.ExecuteReader();
            while(reader.Read())
            {
               rows.Add((new TessageId(reader.GetGuid(0)),
                         reader.GetInt32(1),
                         reader.GetBoolean(2)));
            }

            return rows;
         });

      DiscardUndeliveredTessages(endpointId, [..owed.Select(row => row.TessageId)]);

      return [..owed.Select(row => new ITessagingSqlLayer.DiscardedTessage(
                              tessageId: row.TessageId,
                              typeId: _typeIdInterner.GetTypeId(row.TypeId),
                              wasStranded: row.WasStranded))];
   }

   static string TessageIdParameterList(int tessageIdCount) =>
      string.Join(", ", Enumerable.Range(0, tessageIdCount).Select(index => $"@{DispatchingTable.TessageId}_{index}"));

   public async Task InitAsync() => await _schemaManager.EnsureSchemaInitializedAsync().caf();
}
