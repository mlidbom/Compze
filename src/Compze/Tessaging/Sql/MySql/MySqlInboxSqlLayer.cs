using Compze.Sql.Common;
using Compze.Sql.MySql.SystemExtensions;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Utilities.Contracts;
using Compze.Utilities.Threading.TasksCE;
using MessageTable =  Compze.Tessaging.Hosting.Implementation.IServiceBusSqlLayer.InboxMessageDatabaseSchemaStrings;

namespace Compze.Tessaging.Sql.MySql;

internal partial class MySqlInboxSqlLayer(IMySqlConnectionPool connectionFactory) : IServiceBusSqlLayer.IInboxSqlLayer
{
   readonly IMySqlConnectionPool _connectionFactory = connectionFactory;

   public IServiceBusSqlLayer.SaveMessageResult SaveMessage(Guid messageId, Guid typeId, string serializedMessage)
   {
      return _connectionFactory.UseCommand(
         command =>
         {
            var affectedRows = command
              .SetCommandText(
                  $"""

                   INSERT {MessageTable.TableName} 
                               ({MessageTable.MessageId},  {MessageTable.TypeId},  {MessageTable.Body}, {MessageTable.Status}) 
                       VALUES (@{MessageTable.MessageId}, @{MessageTable.TypeId}, @{MessageTable.Body}, {(int)Inbox.MessageStatus.UnHandled})
                   ON DUPLICATE KEY UPDATE {MessageTable.MessageId} = {MessageTable.MessageId}

                   """)
              .AddParameter(MessageTable.MessageId, messageId)
              .AddParameter(MessageTable.TypeId, typeId)
               //performance: Like with the event store, keep all framework properties out of the JSON and put it into separate columns instead. For events. Reuse a pre-serialized instance from the persisting to the event store.
              .AddMediumTextParameter(MessageTable.Body, serializedMessage)
              .ExecuteNonQuery();

            return affectedRows == 1 
               ? IServiceBusSqlLayer.SaveMessageResult.NewMessage 
               : IServiceBusSqlLayer.SaveMessageResult.Duplicate;
         });
   }

   public void MarkAsSucceeded(Guid messageId)
   {
      _connectionFactory.UseCommand(
         command =>
         {
            var affectedRows = command
                              .SetCommandText(
                                  $"""

                                   UPDATE {MessageTable.TableName} 
                                       SET {MessageTable.Status} = {(int)Inbox.MessageStatus.Succeeded}
                                   WHERE {MessageTable.MessageId} = @{MessageTable.MessageId}
                                       AND {MessageTable.Status} = {(int)Inbox.MessageStatus.UnHandled}

                                   """)
                              .AddParameter(MessageTable.MessageId, messageId)
                              .ExecuteNonQuery();

            Assert.Result.Is(affectedRows == 1);
            return affectedRows;
         });
   }

   public int RecordException(Guid messageId, string exceptionStackTrace, string exceptionMessage, string exceptionType)
   {
      return _connectionFactory.UseCommand(
         command => command
                   .SetCommandText(
                       $"""

                        UPDATE {MessageTable.TableName} 
                            SET {MessageTable.ExceptionCount} = {MessageTable.ExceptionCount} + 1,
                                {MessageTable.ExceptionType} = @{MessageTable.ExceptionType},
                                {MessageTable.ExceptionStackTrace} = @{MessageTable.ExceptionStackTrace},
                                {MessageTable.ExceptionMessage} = @{MessageTable.ExceptionMessage}
                                
                        WHERE {MessageTable.MessageId} = @{MessageTable.MessageId}

                        """)
                   .AddParameter(MessageTable.MessageId, messageId)
                   .AddMediumTextParameter(MessageTable.ExceptionStackTrace, exceptionStackTrace)
                   .AddMediumTextParameter(MessageTable.ExceptionMessage, exceptionMessage)
                   .AddVarcharParameter(MessageTable.ExceptionType, 500, exceptionType)
                   .ExecuteNonQuery());
   }

   public int MarkAsFailed(Guid messageId)
   {
      return _connectionFactory.UseCommand(
         command => command
                   .SetCommandText(
                       $"""

                        UPDATE {MessageTable.TableName} 
                            SET {MessageTable.Status} = {(int)Inbox.MessageStatus.Failed}
                        WHERE {MessageTable.MessageId} = @{MessageTable.MessageId}
                            AND {MessageTable.Status} = {(int)Inbox.MessageStatus.UnHandled}
                        """)
                   .AddParameter(MessageTable.MessageId, messageId)
                   .ExecuteNonQuery());
   }

   public async Task InitAsync() => await SchemaManager.EnsureTablesExistAsync(_connectionFactory).caf();
}