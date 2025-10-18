using Compze.Sql.Common;
using Compze.Sql.MicrosoftSql;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Utilities.Contracts;
using Compze.Utilities.Threading.TasksCE;
using Schema =  Compze.Tessaging.Hosting.Implementation.IServiceBusSqlLayer.InboxMessageDatabaseSchemaStrings;

namespace Compze.Tessaging.Sql.MicrosoftSql;

partial class MsSqlInboxSqlLayer(IMsSqlConnectionPool connectionFactory) : IServiceBusSqlLayer.IInboxSqlLayer
{
   readonly IMsSqlConnectionPool _connectionFactory = connectionFactory;

   public IServiceBusSqlLayer.SaveMessageResult SaveMessage(Guid messageId, Guid typeId, string serializedMessage)
   {
      return _connectionFactory.UseCommand(
         command =>
         {
            var affectedRows = command
              .SetCommandText(
                  $"""
                   MERGE {Schema.TableName} AS target
                   USING (SELECT @{Schema.MessageId} AS {Schema.MessageId}, --create a one row table "source" to be merged if its rows are not already in the table
                                 @{Schema.TypeId} AS {Schema.TypeId}, 
                                 @{Schema.Body} AS {Schema.Body}) AS source
                   ON target.{Schema.MessageId} = source.{Schema.MessageId}
                   WHEN NOT MATCHED THEN
                       INSERT ({Schema.MessageId}, {Schema.TypeId}, {Schema.Body}, {Schema.Status})
                       VALUES (source.{Schema.MessageId}, source.{Schema.TypeId}, source.{Schema.Body}, {(int)Inbox.MessageStatus.UnHandled});

                   """)
              .AddParameter(Schema.MessageId, messageId)
              .AddParameter(Schema.TypeId, typeId)
               //performance: Like with the event store, keep all framework properties out of the JSON and put it into separate columns instead. For events. Reuse a pre-serialized instance from the persisting to the event store.
              .AddNVarcharMaxParameter(Schema.Body, serializedMessage)
              .ExecuteNonQuery();

            return affectedRows == 0 
               ? IServiceBusSqlLayer.SaveMessageResult.Duplicate 
               : IServiceBusSqlLayer.SaveMessageResult.NewMessage;
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

                                   UPDATE {Schema.TableName} 
                                       SET {Schema.Status} = {(int)Inbox.MessageStatus.Succeeded}
                                   WHERE {Schema.MessageId} = @{Schema.MessageId}
                                       AND {Schema.Status} = {(int)Inbox.MessageStatus.UnHandled}

                                   """)
                              .AddParameter(Schema.MessageId, messageId)
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

                        UPDATE {Schema.TableName} 
                            SET {Schema.ExceptionCount} = {Schema.ExceptionCount} + 1,
                                {Schema.ExceptionType} = @{Schema.ExceptionType},
                                {Schema.ExceptionStackTrace} = @{Schema.ExceptionStackTrace},
                                {Schema.ExceptionMessage} = @{Schema.ExceptionMessage}
                                
                        WHERE {Schema.MessageId} = @{Schema.MessageId}

                        """)
                   .AddParameter(Schema.MessageId, messageId)
                   .AddNVarcharMaxParameter(Schema.ExceptionStackTrace, exceptionStackTrace)
                   .AddNVarcharMaxParameter(Schema.ExceptionMessage, exceptionMessage)
                   .AddNVarcharParameter(Schema.ExceptionType, 500, exceptionType)
                   .ExecuteNonQuery());
   }

   public int MarkAsFailed(Guid messageId)
   {
      return _connectionFactory.UseCommand(
         command => command
                   .SetCommandText(
                       $"""

                        UPDATE {Schema.TableName} 
                            SET {Schema.Status} = {(int)Inbox.MessageStatus.Failed}
                        WHERE {Schema.MessageId} = @{Schema.MessageId}
                            AND {Schema.Status} = {(int)Inbox.MessageStatus.UnHandled}
                        """)
                   .AddParameter(Schema.MessageId, messageId)
                   .ExecuteNonQuery());
   }

   public async Task InitAsync() => await SchemaManager.EnsureTablesExistAsync(_connectionFactory).caf();
}
