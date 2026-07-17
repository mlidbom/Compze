using Compze.Abstractions.Public;
using Compze.Abstractions.Hosting.Public;
using Compze.Internals.Sql.Common;
using Compze.Internals.Sql.Sqlite;
using Compze.Internals.Sql.Sqlite.Private;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Transport.SqlLayer;
using Compze.TypeIdentifiers.Interning;
using DispatchingTable = Compze.Tessaging.Transport.SqlLayer.IServiceBusSqlLayer.OutboxTessageDispatchingTableSchemaStrings;
using TessageTable = Compze.Tessaging.Transport.SqlLayer.IServiceBusSqlLayer.OutboxTessagesDatabaseSchemaStrings;

namespace Compze.Tessaging.Sqlite;

partial class SqliteOutboxSqlLayer(ISqliteConnectionPool connectionFactory, SqliteSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) : IServiceBusSqlLayer.IOutboxSqlLayer
{
   readonly ISqliteConnectionPool _connectionFactory = connectionFactory;
   readonly SqliteSqlLayerSchemaManager _schemaManager = schemaManager;
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
              .AddMediumTextParameter(TessageTable.TessageId, tessageWithReceivers.TessageId.ToString())
              .AddParameter(TessageTable.TypeId, internedTypeId)
              .AddMediumTextParameter(TessageTable.SerializedTessage, tessageWithReceivers.SerializedTessage)
              .AddParameter(DispatchingTable.IsReceived, 0);

            tessageWithReceivers.ReceiverEndpointIds.ForEach(
               (endpointId, index)
                  => command.AppendCommandText($"""

                                                INSERT INTO {DispatchingTable.TableName} 
                                                            ({DispatchingTable.TessageId},  {DispatchingTable.EndpointId},          {DispatchingTable.IsReceived}) 
                                                    VALUES (@{DispatchingTable.TessageId}, @{DispatchingTable.EndpointId}_{index}, @{DispatchingTable.IsReceived});

                                                """).AddMediumTextParameter($"{DispatchingTable.EndpointId}_{index}", endpointId.ToString()));

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
                   .AddMediumTextParameter(DispatchingTable.TessageId, tessageId.ToString())
                   .AddMediumTextParameter(DispatchingTable.EndpointId, endpointId.ToString())
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
                   .AddMediumTextParameter(DispatchingTable.TessageId, tessageId.ToString())
                   .AddMediumTextParameter(DispatchingTable.EndpointId, endpointId.ToString())
                   .AddMediumTextParameter(DispatchingTable.LastAttemptTime, DateTime.UtcNow.ToString("O"))
                   .AddMediumTextParameter(DispatchingTable.FailureReason, failureReason)
                   .ExecuteNonQuery());
   }

   public IReadOnlyList<IServiceBusSqlLayer.UndeliveredTessage> GetUndeliveredTessagesForEndpoint(EndpointId endpointId)
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
                      AND d.{DispatchingTable.IsStranded} = 0 --A stranded tessage waits for explicit resolution on the decommission surface, never for delivery.
                      AND d.{DispatchingTable.EndpointId} = @endpointId
                    ORDER BY m.{TessageTable.GeneratedId} --Send order: recovery re-establishes in-order delivery, oldest undelivered first.

                    """)
               .AddMediumTextParameter("endpointId", endpointId.ToString());

            using var reader = command.ExecuteReader();
            while(reader.Read())
            {
               rows.Add((new TessageId(reader.GetGuidFromString(0)),
                         reader.GetInt32(1),
                         reader.GetString(2)));
            }

            return rows;
         });

      return [..raw.Select(row => new IServiceBusSqlLayer.UndeliveredTessage(
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
              .AddMediumTextParameter(DispatchingTable.EndpointId, endpointId.ToString());
            tessageIds.ForEach((tessageId, index) => command.AddMediumTextParameter($"{DispatchingTable.TessageId}_{index}", tessageId.ToString()));
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
              .AddMediumTextParameter(DispatchingTable.EndpointId, endpointId.ToString());
            tessageIds.ForEach((tessageId, index) => command.AddMediumTextParameter($"{DispatchingTable.TessageId}_{index}", tessageId.ToString()));
            command.ExecuteNonQuery();
         });
   }

   public IReadOnlyList<IServiceBusSqlLayer.DiscardedTessage> DiscardAllTessagesOwedTo(EndpointId endpointId)
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
               .AddMediumTextParameter("endpointId", endpointId.ToString());

            using var reader = command.ExecuteReader();
            while(reader.Read())
            {
               rows.Add((new TessageId(reader.GetGuidFromString(0)),
                         reader.GetInt32(1),
                         reader.GetBoolean(2)));
            }

            return rows;
         });

      DiscardUndeliveredTessages(endpointId, [..owed.Select(row => row.TessageId)]);

      return [..owed.Select(row => new IServiceBusSqlLayer.DiscardedTessage(
                              tessageId: row.TessageId,
                              typeId: _typeIdInterner.GetTypeId(row.TypeId),
                              wasStranded: row.WasStranded))];
   }

   static string TessageIdParameterList(int tessageIdCount) =>
      string.Join(", ", Enumerable.Range(0, tessageIdCount).Select(index => $"@{DispatchingTable.TessageId}_{index}"));

   public async Task InitAsync() => await _schemaManager.EnsureSchemaInitializedAsync().caf();
}
