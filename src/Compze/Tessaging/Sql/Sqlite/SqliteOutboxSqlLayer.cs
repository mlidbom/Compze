using Compze.Sql.Common;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Utilities.SystemCE.LinqCE;
using System.Globalization;
using Compze.Sql.Sqlite;
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
                       VALUES (@{MessageTable.MessageId}, @{MessageTable.TypeIdGuidValue}, @{MessageTable.SerializedMessage});

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
                                                    VALUES (@{DispatchingTable.MessageId}, @{DispatchingTable.EndpointId}_{index}, @{DispatchingTable.IsReceived});

                                                """).AddVarcharParameter($"{DispatchingTable.EndpointId}_{index}", 36, endpointId.ToString()));

            command.ExecuteNonQuery();
         });
   }

   public IServiceBusSqlLayer.MarkAsReceivedResult MarkAsReceived(Guid messageId, Guid endpointId)
   {
      var affectedRows = _connectionFactory.UseCommand(
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

      return affectedRows == 1
                ? IServiceBusSqlLayer.MarkAsReceivedResult.Initial
                : IServiceBusSqlLayer.MarkAsReceivedResult.WasAlreadyMarked;
   }

   public void RecordDeliveryFailure(Guid messageId, Guid endpointId, string failureReason)
   {
      _connectionFactory.UseCommand(
         command => command
                   .SetCommandText(
                       $"""

                        UPDATE {DispatchingTable.TableName} 
                            SET {DispatchingTable.RetryCount} = {DispatchingTable.RetryCount} + 1,
                                {DispatchingTable.LastAttemptTime} = @{DispatchingTable.LastAttemptTime},
                                {DispatchingTable.FailureReason} = @{DispatchingTable.FailureReason}
                        WHERE {DispatchingTable.MessageId} = @{DispatchingTable.MessageId}
                            AND {DispatchingTable.EndpointId} = @{DispatchingTable.EndpointId}

                        """)
                   .AddVarcharParameter(DispatchingTable.MessageId, 36, messageId.ToString())
                   .AddVarcharParameter(DispatchingTable.EndpointId, 36, endpointId.ToString())
                   .AddVarcharParameter(DispatchingTable.LastAttemptTime, 50, DateTime.UtcNow.ToString("O"))
                   .AddMediumTextParameter(DispatchingTable.FailureReason, failureReason)
                   .ExecuteNonQuery());
   }

   public IReadOnlyList<IServiceBusSqlLayer.UndeliveredMessage> GetUndeliveredMessages(TimeSpan olderThan)
   {
      var cutoffTime = DateTime.UtcNow - olderThan;
      
      return _connectionFactory.UseCommand(
         command =>
         {
            var messages = new List<IServiceBusSqlLayer.UndeliveredMessage>();
            
            command
               .SetCommandText(
                   $"""

                    SELECT m.{MessageTable.MessageId}, 
                           m.{MessageTable.TypeIdGuidValue}, 
                           m.{MessageTable.SerializedMessage},
                           d.{DispatchingTable.EndpointId},
                           d.{DispatchingTable.RetryCount},
                           d.{DispatchingTable.LastAttemptTime}
                    FROM {MessageTable.TableName} m
                    INNER JOIN {DispatchingTable.TableName} d ON m.{MessageTable.MessageId} = d.{DispatchingTable.MessageId}
                    WHERE d.{DispatchingTable.IsReceived} = 0
                      AND (d.{DispatchingTable.LastAttemptTime} IS NULL 
                           OR d.{DispatchingTable.LastAttemptTime} < @cutoffTime)
                    ORDER BY d.{DispatchingTable.RetryCount}, d.{DispatchingTable.LastAttemptTime}

                    """)
               .AddVarcharParameter("cutoffTime", 50, cutoffTime.ToString("O"));
            
            using var reader = command.ExecuteReader();
            while(reader.Read())
            {
               messages.Add(new IServiceBusSqlLayer.UndeliveredMessage(
                  messageId: Guid.Parse(reader.GetString(0)),
                  typeIdGuid: Guid.Parse(reader.GetString(1)),
                  serializedMessage: reader.GetString(2),
                  targetEndpointId: Guid.Parse(reader.GetString(3)),
                  retryCount: reader.GetInt32(4),
                  lastAttemptTime: reader.IsDBNull(5) ? null : DateTime.Parse(reader.GetString(5), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)));
            }
            
            return messages;
         });
   }

   public Task InitAsync() => SchemaManager.EnsureTablesExistAsync(_connectionFactory);
}
