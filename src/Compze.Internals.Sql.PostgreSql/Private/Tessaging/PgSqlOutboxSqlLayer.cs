using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Internal.SqlLayer;
using Compze.Internals.Sql.Common;
using Compze.Internals.SystemCE.LinqCE;
using NpgsqlTypes;
using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using DispatchingTable = Compze.Core.Tessaging.Internal.SqlLayer.IServiceBusSqlLayer.OutboxTessageDispatchingTableSchemaStrings;
using TessageTable = Compze.Core.Tessaging.Internal.SqlLayer.IServiceBusSqlLayer.OutboxTessagesDatabaseSchemaStrings;

namespace Compze.Internals.Sql.PostgreSql.Private.Tessaging;

partial class PgSqlOutboxSqlLayer(IPgSqlConnectionPool connectionFactory, PgSqlSqlLayerSchemaManager schemaManager) : IServiceBusSqlLayer.IOutboxSqlLayer
{
   readonly IPgSqlConnectionPool _connectionFactory = connectionFactory;
   readonly PgSqlSqlLayerSchemaManager _schemaManager = schemaManager;

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
              .AddParameter(TessageTable.TessageId, tessageWithReceivers.TessageId.Value)
              .AddParameter(TessageTable.TypeIdGuidValue, tessageWithReceivers.TypeId.GuidValue)
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
                      AND d.{DispatchingTable.EndpointId} = @endpointId
                    ORDER BY d.{DispatchingTable.RetryCount}, d.{DispatchingTable.LastAttemptTime} NULLS FIRST;

                    """)
               .AddParameter("endpointId", endpointId.Value)
               .PrepareStatement();

            using var reader = command.ExecuteReader();
            while(reader.Read())
            {
               tessages.Add(new IServiceBusSqlLayer.UndeliveredTessage(
                  tessageId: new TessageId(reader.GetGuid(0)),
                  typeId: new MappedTypeId(reader.GetGuid(1)),
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