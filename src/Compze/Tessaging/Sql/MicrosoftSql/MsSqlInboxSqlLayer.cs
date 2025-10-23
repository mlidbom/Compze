using System;
using System.Threading.Tasks;
using Compze.Sql.Common;
using Compze.Sql.MicrosoftSql;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Utilities.Contracts;
using Compze.Utilities.Threading.TasksCE;
using MessageTable = Compze.Tessaging.Hosting.Implementation.IServiceBusSqlLayer.InboxMessageDatabaseSchemaStrings;

namespace Compze.Tessaging.Sql.MicrosoftSql;

partial class MsSqlInboxSqlLayer(IMsSqlConnectionPool connectionFactory) : IServiceBusSqlLayer.IInboxSqlLayer
{
   readonly IMsSqlConnectionPool _connectionFactory = connectionFactory;

   public IServiceBusSqlLayer.SaveMessageResult SaveMessage(Guid messageId, Guid typeId, string serializedMessage)
   {
      return _connectionFactory.UseCommand(command =>
      {
         var affectedRows = command
                           .SetCommandText(
                               $"""
                                MERGE {MessageTable.TableName} AS target
                                USING (SELECT @{MessageTable.MessageId} AS {MessageTable.MessageId}, --create a one row table "source" to be merged if its rows are not already in the table
                                              @{MessageTable.TypeId} AS {MessageTable.TypeId}, 
                                              @{MessageTable.Body} AS {MessageTable.Body}) AS source
                                ON target.{MessageTable.MessageId} = source.{MessageTable.MessageId}
                                WHEN NOT MATCHED THEN
                                    INSERT ({MessageTable.MessageId}, {MessageTable.TypeId}, {MessageTable.Body}, {MessageTable.Status})
                                    VALUES (source.{MessageTable.MessageId}, source.{MessageTable.TypeId}, source.{MessageTable.Body}, {(int)Inbox.MessageStatus.UnHandled});

                                """)
                           .AddParameter(MessageTable.MessageId, messageId)
                           .AddParameter(MessageTable.TypeId, typeId)
                            //performance: Like with the event store, keep all framework properties out of the JSON and put it into separate columns instead. For events. Reuse a pre-serialized instance from the persisting to the event store.
                           .AddNVarcharMaxParameter(MessageTable.Body, serializedMessage)
                           .ExecuteNonQuery();

         return affectedRows == 0
                   ? IServiceBusSqlLayer.SaveMessageResult.Duplicate
                   : IServiceBusSqlLayer.SaveMessageResult.NewMessage;
      });
   }

   public void MarkAsSucceeded(Guid messageId)
   {
      _connectionFactory.UseCommand(command =>
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
      return _connectionFactory.UseCommand(command => command
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
                                                     .AddNVarcharMaxParameter(MessageTable.ExceptionStackTrace, exceptionStackTrace)
                                                     .AddNVarcharMaxParameter(MessageTable.ExceptionMessage, exceptionMessage)
                                                     .AddNVarcharParameter(MessageTable.ExceptionType, 500, exceptionType)
                                                     .ExecuteNonQuery());
   }

   public int MarkAsFailed(Guid messageId)
   {
      return _connectionFactory.UseCommand(command => command
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
