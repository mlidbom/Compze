using Compze.Sql.Common;
using Compze.Sql.Sqlite.Infrastructure;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Utilities.SystemCE.LinqCE;
using MessageTable = Compze.Tessaging.Hosting.Implementation.IServiceBusSqlLayer.OutboxMessagesDatabaseSchemaStrings;
using DispatchingTable = Compze.Tessaging.Hosting.Implementation.IServiceBusSqlLayer.OutboxMessageDispatchingTableSchemaStrings;

namespace Compze.Tessaging.Sql.Sqlite;

partial class SqliteOutboxSqlLayer(ISqliteConnectionPool connectionFactory) : IServiceBusSqlLayer.IOutboxSqlLayer
{
   readonly ISqliteConnectionPool _connectionFactory = connectionFactory;

   public void SaveMessage(IServiceBusSqlLayer.OutboxMessageWithReceivers messageWithReceivers)
   {
      _connectionFactory.UseCommand(
         command =>
         {
            command
              .SetCommandText(
                  $"""

                   INSERT INTO {MessageTable.TableName} 
                               ({MessageTable.MessageId},  {MessageTable.TypeIdGuidValue}, {MessageTable.SerializedMessage}) 
                       VALUES (@{MessageTable.MessageId}, @{MessageTable.TypeIdGuidValue}, @{MessageTable.SerializedMessage})

                   """)
              .AddVarcharParameter(MessageTable.MessageId, 36, messageWithReceivers.MessageId.ToString())
              .AddVarcharParameter(MessageTable.TypeIdGuidValue, 36, messageWithReceivers.TypeIdGuidValue.ToString())
              .AddMediumTextParameter(MessageTable.SerializedMessage, messageWithReceivers.SerializedMessage)
              .AddParameter(DispatchingTable.IsReceived, 0);

            messageWithReceivers.ReceiverEndpointIds.ForEach(
               (endpointId, index)
                  => command.AppendCommandText($"""

                                                INSERT INTO {DispatchingTable.TableName} 
                                                            ({DispatchingTable.MessageId},  {DispatchingTable.EndpointId},          {DispatchingTable.IsReceived}) 
                                                    VALUES (@{DispatchingTable.MessageId}, @{DispatchingTable.EndpointId}_{index}, @{DispatchingTable.IsReceived})

                                                """).AddVarcharParameter($"{DispatchingTable.EndpointId}_{index}", 36, endpointId.ToString()));

            command.ExecuteNonQuery();
         });
   }

   public int MarkAsReceived(Guid messageId, Guid endpointId)
   {
      return _connectionFactory.UseCommand(
         command => command
                   .SetCommandText(
                       $"""

                        UPDATE {DispatchingTable.TableName} 
                            SET {DispatchingTable.IsReceived} = 1
                        WHERE {DispatchingTable.MessageId} = @{DispatchingTable.MessageId}
                            AND {DispatchingTable.EndpointId} = @{DispatchingTable.EndpointId}
                            AND {DispatchingTable.IsReceived} = 0

                        """)
                   .AddVarcharParameter(DispatchingTable.MessageId, 36, messageId.ToString())
                   .AddVarcharParameter(DispatchingTable.EndpointId, 36, endpointId.ToString())
                   .ExecuteNonQuery());
   }

   public Task InitAsync() => SchemaManager.EnsureTablesExistAsync(_connectionFactory);
}
