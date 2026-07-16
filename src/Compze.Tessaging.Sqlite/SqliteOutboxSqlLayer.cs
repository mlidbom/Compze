using Compze.Abstractions.Public;
using Compze.Abstractions.Hosting.Public;
using Compze.Internals.Sql.Common;
using Compze.Internals.Sql.Sqlite;
using Compze.Internals.Sql.Sqlite.Private;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Transport.SqlLayer;
using Compze.TypeIdentifiers;
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

   //A tevent's receiver row was bound at publish; a tommand has none until here - the delivery that succeeds binds it
   //(route-at-delivery), recording who actually received it. Insert-the-row-if-missing then flip-if-unreceived keeps the
   //affected-rows contract uniform across both kinds: > 0 means newly marked, 0 means it was already marked.
   public IServiceBusSqlLayer.MarkAsReceivedResult MarkAsReceived(TessageId tessageId, EndpointId endpointId)
   {
      var affectedRows = _connectionFactory.UseCommand(
         command => command
                   .SetCommandText(
                       $"""

                        INSERT INTO {DispatchingTable.TableName}
                                    ({DispatchingTable.TessageId},  {DispatchingTable.EndpointId},  {DispatchingTable.IsReceived})
                            VALUES (@{DispatchingTable.TessageId}, @{DispatchingTable.EndpointId}, 0)
                        ON CONFLICT({DispatchingTable.TessageId}, {DispatchingTable.EndpointId}) DO NOTHING;

                        UPDATE {DispatchingTable.TableName}
                            SET {DispatchingTable.IsReceived} = 1
                        WHERE {DispatchingTable.TessageId} = @{DispatchingTable.TessageId}
                            AND {DispatchingTable.EndpointId} = @{DispatchingTable.EndpointId}
                            AND {DispatchingTable.IsReceived} = 0

                        """)
                   .AddMediumTextParameter(DispatchingTable.TessageId, tessageId.ToString())
                   .AddMediumTextParameter(DispatchingTable.EndpointId, endpointId.ToString())
                   .ExecuteNonQuery());

      return affectedRows > 0
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

   public IReadOnlyList<IServiceBusSqlLayer.UndeliveredTessage> GetUndeliveredTessagesForEndpoint(EndpointId endpointId, IReadOnlyCollection<TypeId> handledTommandTypes)
   {
      // Intern before opening a connection: interning may hit the database, and nesting a second connection
      // inside a held one deadlocks the pool.
      var internedTommandTypeIds = handledTommandTypes.Select(_typeIdInterner.GetOrInternId).ToList();

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
                    LEFT JOIN {DispatchingTable.TableName} d ON m.{TessageTable.TessageId} = d.{DispatchingTable.TessageId}
                    WHERE {UndeliveredForEndpointCondition(internedTommandTypeIds)}
                    ORDER BY m.{TessageTable.GeneratedId} --Send order: recovery re-establishes in-order delivery, oldest undelivered first.

                    """)
               .AddMediumTextParameter("endpointId", endpointId.ToString());

            internedTommandTypeIds.ForEach((internedTypeId, index) => command.AddParameter($"HandledTommandType_{index}", internedTypeId));

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

   //Two kinds of undelivered work ride one query so a single ORDER BY GeneratedId preserves send order across them: rows
   //bound to this endpoint and not yet received, and unbound tommands - no dispatching row at all until the delivery that
   //succeeds binds one - whose type the endpoint's advertisement handles (route-at-delivery).
   static string UndeliveredForEndpointCondition(IReadOnlyList<int> internedTommandTypeIds)
   {
      const string boundToThisEndpoint = $"(d.{DispatchingTable.EndpointId} = @endpointId AND d.{DispatchingTable.IsReceived} = 0)";
      if(internedTommandTypeIds.Count == 0) return boundToThisEndpoint;

      var handledTommandTypeParameters = string.Join(", ", internedTommandTypeIds.Select((_, index) => $"@HandledTommandType_{index}"));
      return $"{boundToThisEndpoint} OR (d.{DispatchingTable.TessageId} IS NULL AND m.{TessageTable.TypeId} IN ({handledTommandTypeParameters}))";
   }

   public async Task InitAsync() => await _schemaManager.EnsureSchemaInitializedAsync().caf();
}
