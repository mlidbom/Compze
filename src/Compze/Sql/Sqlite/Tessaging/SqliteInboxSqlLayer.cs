using System;
using System.Threading.Tasks;
using Compze.Sql.Common;
using Compze.Sql.Sqlite;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Utilities.Contracts;
using Compze.Utilities.Threading.TasksCE;
using MessageTable =  Compze.Tessaging.Hosting.Implementation.IServiceBusSqlLayer.InboxMessageDatabaseSchemaStrings;

namespace Compze.Tessaging.Sql.Sqlite;

partial class SqliteInboxSqlLayer(ISqliteConnectionPool connectionFactory) : IServiceBusSqlLayer.IInboxSqlLayer
{
   readonly ISqliteConnectionPool _connectionFactory = connectionFactory;

   public IServiceBusSqlLayer.SaveMessageResult SaveMessage(Guid messageId, Guid typeId, string serializedMessage)
   {
      return _connectionFactory.UseCommand(
         command =>
         {
            var affectedRows = command
              .SetCommandText(
                  $"""

                   INSERT INTO {MessageTable.TableName} 
                               ({MessageTable.MessageId},  {MessageTable.TypeId},  {MessageTable.Body}, {MessageTable.Status}) 
                       VALUES (@{MessageTable.MessageId}, @{MessageTable.TypeId}, @{MessageTable.Body}, {(int)InboxMessageStatus.UnHandled})
                   ON CONFLICT ({MessageTable.MessageId}) DO NOTHING

                   """)
              .AddVarcharParameter(MessageTable.MessageId, 36, messageId.ToString())
              .AddVarcharParameter(MessageTable.TypeId, 36, typeId.ToString())
              .AddMediumTextParameter(MessageTable.Body, serializedMessage)
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

                                   UPDATE {MessageTable.TableName} 
                                       SET {MessageTable.Status} = {(int)InboxMessageStatus.Succeeded}
                                   WHERE {MessageTable.MessageId} = @{MessageTable.MessageId}
                                       AND {MessageTable.Status} = {(int)InboxMessageStatus.UnHandled}

                                   """)
                              .AddVarcharParameter(MessageTable.MessageId, 36, messageId.ToString())
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
                   .AddVarcharParameter(MessageTable.MessageId, 36, messageId.ToString())
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
                            SET {MessageTable.Status} = {(int)InboxMessageStatus.Failed}
                        WHERE {MessageTable.MessageId} = @{MessageTable.MessageId}
                            AND {MessageTable.Status} = {(int)InboxMessageStatus.UnHandled}
                        """)
                   .AddVarcharParameter(MessageTable.MessageId, 36, messageId.ToString())
                   .ExecuteNonQuery());
   }

   public async Task InitAsync() => await SchemaManager.EnsureTablesExistAsync(_connectionFactory).caf();
}
