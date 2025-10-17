using Compze.Sql.Sqlite;
using Compze.Utilities.Threading.TasksCE;
using Message = Compze.Tessaging.Hosting.Implementation.IServiceBusSqlLayer.OutboxMessagesDatabaseSchemaStrings;
using D = Compze.Tessaging.Hosting.Implementation.IServiceBusSqlLayer.OutboxMessageDispatchingTableSchemaStrings;

namespace Compze.Tessaging.Sql.Sqlite;

partial class SqliteOutboxSqlLayer
{
   static class SchemaManager
   {
      public static async Task EnsureTablesExistAsync(ISqliteConnectionPool connectionFactory)
      {
         await connectionFactory.ExecuteNonQueryAsync($"""

                                                       CREATE TABLE IF NOT EXISTS {Message.TableName}
                                                       (
                                                           {Message.GeneratedId}       INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                                                           {Message.TypeIdGuidValue}   TEXT                              NOT NULL,
                                                           {Message.MessageId}         TEXT                              NOT NULL UNIQUE,
                                                           {Message.SerializedMessage} TEXT                              NOT NULL
                                                       );

                                                       CREATE TABLE IF NOT EXISTS {D.TableName}
                                                       (
                                                           {D.MessageId}        TEXT    NOT NULL,
                                                           {D.EndpointId}       TEXT    NOT NULL,
                                                           {D.IsReceived}       INTEGER NOT NULL,
                                                           {D.RetryCount}       INTEGER NOT NULL DEFAULT 0,
                                                           {D.LastAttemptTime}  TEXT    NULL,
                                                           {D.FailureReason}    TEXT    NULL,

                                                           PRIMARY KEY( {D.MessageId}, {D.EndpointId}),
                                                           FOREIGN KEY ( {D.MessageId} ) REFERENCES {Message.TableName} ({Message.MessageId})
                                                       );

                                                       """).caf();
      }
   }
}
