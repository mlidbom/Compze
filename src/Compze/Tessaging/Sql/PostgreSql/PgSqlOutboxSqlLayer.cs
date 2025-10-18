using Compze.Sql.Common;
using Compze.Sql.PostgreSql;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Utilities.SystemCE.LinqCE;
using NpgsqlTypes;
using MessageTable = Compze.Tessaging.Hosting.Implementation.IServiceBusSqlLayer.OutboxMessagesDatabaseSchemaStrings;
using DispatchingTable = Compze.Tessaging.Hosting.Implementation.IServiceBusSqlLayer.OutboxMessageDispatchingTableSchemaStrings;

namespace Compze.Tessaging.Sql.PostgreSql;

partial class PgSqlOutboxSqlLayer(IPgSqlConnectionPool connectionFactory) : IServiceBusSqlLayer.IOutboxSqlLayer
{
   readonly IPgSqlConnectionPool _connectionFactory = connectionFactory;

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
              .AddParameter(MessageTable.MessageId, messageWithReceivers.MessageId)
              .AddParameter(MessageTable.TypeIdGuidValue, messageWithReceivers.TypeIdGuidValue)
               //performance: Like with the event store, keep all framework properties out of the JSON and put it into separate columns instead. For events. Reuse a pre-serialized instance from the persisting to the event store.
              .AddMediumTextParameter(MessageTable.SerializedMessage, messageWithReceivers.SerializedMessage)
              .AddParameter(DispatchingTable.IsReceived, NpgsqlDbType.Boolean, false);

            messageWithReceivers.ReceiverEndpointIds.ForEach(
               (endpointId, index)
                  => command.AppendCommandText($"""

                                                INSERT INTO {DispatchingTable.TableName} 
                                                            ({DispatchingTable.MessageId},  {DispatchingTable.EndpointId},          {DispatchingTable.IsReceived}) 
                                                    VALUES (@{DispatchingTable.MessageId}, @{DispatchingTable.EndpointId}_{index}, @{DispatchingTable.IsReceived});

                                                """).AddParameter($"{DispatchingTable.EndpointId}_{index}", endpointId));

            command
              .PrepareStatement() //Even though the count above will differ it will probably not have too many variations for preparing the statement to be quite beneficial.
              .ExecuteNonQuery();
         });
   }

   public IServiceBusSqlLayer.MarkAsReceivedResult MarkAsReceived(Guid messageId, Guid endpointId)
   {
      var affectedRows = _connectionFactory.UseCommand(
         command => command
                   .SetCommandText(
                       $"""

                        UPDATE {DispatchingTable.TableName} 
                            SET {DispatchingTable.IsReceived} = true
                        WHERE {DispatchingTable.MessageId} = @{DispatchingTable.MessageId}
                            AND {DispatchingTable.EndpointId} = @{DispatchingTable.EndpointId}
                            AND {DispatchingTable.IsReceived} = false;

                        """)
                   .AddParameter(DispatchingTable.MessageId, messageId)
                   .AddParameter(DispatchingTable.EndpointId, endpointId)
                   .PrepareStatement()
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
                            AND {DispatchingTable.EndpointId} = @{DispatchingTable.EndpointId};

                        """)
                   .AddParameter(DispatchingTable.MessageId, messageId)
                   .AddParameter(DispatchingTable.EndpointId, endpointId)
                   .AddTimestampWithTimeZone(DispatchingTable.LastAttemptTime, DateTime.UtcNow)
                   .AddMediumTextParameter(DispatchingTable.FailureReason, failureReason)
                   .PrepareStatement()
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
                    WHERE d.{DispatchingTable.IsReceived} = false
                      AND (d.{DispatchingTable.LastAttemptTime} IS NULL 
                           OR d.{DispatchingTable.LastAttemptTime} < @cutoffTime)
                    ORDER BY d.{DispatchingTable.RetryCount}, d.{DispatchingTable.LastAttemptTime} NULLS FIRST;

                    """)
               .AddTimestampWithTimeZone("cutoffTime", cutoffTime)
               .PrepareStatement();
            
            using var reader = command.ExecuteReader();
            while(reader.Read())
            {
               messages.Add(new IServiceBusSqlLayer.UndeliveredMessage(
                  messageId: reader.GetGuid(0),
                  typeIdGuid: reader.GetGuid(1),
                  serializedMessage: reader.GetString(2),
                  targetEndpointId: reader.GetGuid(3),
                  retryCount: reader.GetInt32(4),
                  lastAttemptTime: reader.IsDBNull(5) ? null : reader.GetDateTime(5)));
            }
            
            return messages;
         });
   }

   public Task InitAsync() => SchemaManager.EnsureTablesExistAsync(_connectionFactory);
}