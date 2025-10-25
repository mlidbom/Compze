using System.Threading.Tasks;
using Compze.Sql.Sqlite;
using Compze.Utilities.Threading.TasksCE;
using Tessage =  Compze.Tessaging.Hosting.Implementation.IServiceBusSqlLayer.InboxTessageDatabaseSchemaStrings;

namespace Compze.Tessaging.Sql.Sqlite;

partial class SqliteInboxSqlLayer
{
   static class SchemaManager
   {
      public static async Task EnsureTablesExistAsync(ISqliteConnectionPool connectionFactory)
      {
         await connectionFactory.ExecuteNonQueryAsync($"""

                                            CREATE TABLE IF NOT EXISTS {Tessage.TableName}
                                            (
                                                {Tessage.GeneratedId}         INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                                                {Tessage.TypeId}              TEXT                              NOT NULL,
                                                {Tessage.TessageId}           TEXT                              NOT NULL UNIQUE,
                                                {Tessage.Status}              INTEGER                           NOT NULL,
                                                {Tessage.Body}                TEXT                              NOT NULL,
                                                {Tessage.ExceptionCount}      INTEGER                           NOT NULL DEFAULT 0,
                                                {Tessage.ExceptionType}       TEXT                              NULL,
                                                {Tessage.ExceptionStackTrace} TEXT                              NULL,
                                                {Tessage.ExceptionTessage}    TEXT                              NULL
                                            )

                                            """).caf();
      }
   }
}
