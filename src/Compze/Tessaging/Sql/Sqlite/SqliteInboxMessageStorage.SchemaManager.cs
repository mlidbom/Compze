using Compze.Sql.Sqlite;
using Compze.Utilities.Threading.TasksCE;
using Message =  Compze.Tessaging.Hosting.Implementation.IServiceBusSqlLayer.InboxMessageDatabaseSchemaStrings;

namespace Compze.Tessaging.Sql.Sqlite;

partial class SqliteInboxSqlLayer
{
   static class SchemaManager
   {
      public static async Task EnsureTablesExistAsync(ISqliteConnectionPool connectionFactory)
      {
         await connectionFactory.ExecuteNonQueryAsync($"""

                                            CREATE TABLE IF NOT EXISTS {Message.TableName}
                                            (
                                                {Message.GeneratedId}         INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                                                {Message.TypeId}              TEXT                              NOT NULL,
                                                {Message.MessageId}           TEXT                              NOT NULL UNIQUE,
                                                {Message.Status}              INTEGER                           NOT NULL,
                                                {Message.Body}                TEXT                              NOT NULL,
                                                {Message.ExceptionCount}      INTEGER                           NOT NULL DEFAULT 0,
                                                {Message.ExceptionType}       TEXT                              NULL,
                                                {Message.ExceptionStackTrace} TEXT                              NULL,
                                                {Message.ExceptionMessage}    TEXT                              NULL
                                            )

                                            """).caf();
      }
   }
}
