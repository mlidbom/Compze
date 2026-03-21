using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Internal.SqlLayer;
using Compze.Internals.Sql.Common;
using Compze.Internals.SystemCE.LinqCE;
using System.Globalization;
using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using DispatchingTable = Compze.Core.Tessaging.Internal.SqlLayer.IServiceBusSqlLayer.OutboxTessageDispatchingTableSchemaStrings;
using TessageTable = Compze.Core.Tessaging.Internal.SqlLayer.IServiceBusSqlLayer.OutboxTessagesDatabaseSchemaStrings;

namespace Compze.Internals.Sql.Sqlite.Private.Tessaging;

partial class SqliteOutboxSqlLayer(ISqliteConnectionPool connectionFactory, SqliteSqlLayerSchemaManager schemaManager) : IServiceBusSqlLayer.IOutboxSqlLayer
{
   readonly ISqliteConnectionPool _connectionFactory = connectionFactory;
   readonly SqliteSqlLayerSchemaManager _schemaManager = schemaManager;

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
              .AddMediumTextParameter(TessageTable.TessageId, tessageWithReceivers.TessageId.ToString())
              .AddMediumTextParameter(TessageTable.TypeIdGuidValue, tessageWithReceivers.TypeId.GuidValue.ToString())
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
               .AddMediumTextParameter("endpointId", endpointId.ToString());

            using var reader = command.ExecuteReader();
            while(reader.Read())
            {
               tessages.Add(new IServiceBusSqlLayer.UndeliveredTessage(
                  tessageId: new TessageId(reader.GetGuidFromString(0)),
                  typeId: new MappedTypeId(reader.GetGuidFromString(1)),
                  serializedTessage: reader.GetString(2),
                  targetEndpointId: new EndpointId(reader.GetGuidFromString(3)),
                  retryCount: reader.GetInt32(4),
                  lastAttemptTime: reader.IsDBNull(5) ? null : DateTime.Parse(reader.GetString(5), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)));
            }

            return tessages;
         });
   }

   public async Task InitAsync() => await _schemaManager.EnsureSchemaInitializedAsync().caf();
}
