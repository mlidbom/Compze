using Compze.Sql.Common;
using Compze.Sql.Sqlite.Infrastructure;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Utilities.Contracts;
using Compze.Utilities.Threading.TasksCE;
using Schema =  Compze.Tessaging.Hosting.Implementation.IServiceBusSqlLayer.InboxMessageDatabaseSchemaStrings;

namespace Compze.Tessaging.Sql.Sqlite;

partial class SqliteInboxSqlLayer(ISqliteConnectionPool connectionFactory) : IServiceBusSqlLayer.IInboxSqlLayer
{
   readonly ISqliteConnectionPool _connectionFactory = connectionFactory;

   public void SaveMessage(Guid messageId, Guid typeId, string serializedMessage)
   {
      _connectionFactory.UseCommand(
         command =>
         {
            command
              .SetCommandText(
                  $"""

                   INSERT INTO {Schema.TableName} 
                               ({Schema.MessageId},  {Schema.TypeId},  {Schema.Body}, {Schema.Status}) 
                       VALUES (@{Schema.MessageId}, @{Schema.TypeId}, @{Schema.Body}, {(int)Inbox.MessageStatus.UnHandled})

                   """)
              .AddVarcharParameter(Schema.MessageId, 36, messageId.ToString())
              .AddVarcharParameter(Schema.TypeId, 36, typeId.ToString())
              .AddMediumTextParameter(Schema.Body, serializedMessage)
              .ExecuteNonQuery();
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
                              .AddVarcharParameter(Schema.MessageId, 36, messageId.ToString())
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
                   .AddVarcharParameter(Schema.MessageId, 36, messageId.ToString())
                   .AddMediumTextParameter(Schema.ExceptionStackTrace, exceptionStackTrace)
                   .AddMediumTextParameter(Schema.ExceptionMessage, exceptionMessage)
                   .AddVarcharParameter(Schema.ExceptionType, 500, exceptionType)
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
                   .AddVarcharParameter(Schema.MessageId, 36, messageId.ToString())
                   .ExecuteNonQuery());
   }

   public async Task InitAsync() => await SchemaManager.EnsureTablesExistAsync(_connectionFactory).caf();
}
