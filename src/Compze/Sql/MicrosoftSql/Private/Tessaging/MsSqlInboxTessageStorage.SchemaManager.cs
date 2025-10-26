using System.Threading.Tasks;
using Compze.Utilities.SystemCE.TransactionsCE;
using Compze.Utilities.Threading.TasksCE;
using Tessage = Compze.Core.Tessaging.Internal.SqlLayer.IServiceBusSqlLayer.InboxTessageDatabaseSchemaStrings;

namespace Compze.Sql.MicrosoftSql.Private.Tessaging;

partial class MsSqlInboxSqlLayer
{
   static class SchemaManager
   {
      public static async Task EnsureTablesExistAsync(IMsSqlConnectionPool connectionFactory) =>
         await TransactionScopeCe.SuppressAmbientAsync(async () =>
                                                          //Performance: Why is the TessageId not the primary key? Are we worried about performance loss because of fragmentation because of non-sequential Guids? Is there a (performant and truly reliable) sequential-guid-generator we could use? How does it not being the clustered index impact row vs page etc locking?
                                                          await connectionFactory.ExecuteNonQueryAsync(
                                                             $"""

                                                              IF NOT EXISTS(select name from sys.tables where name = '{Tessage.TableName}')
                                                              BEGIN
                                                                  CREATE TABLE {Tessage.TableName}
                                                                  (
                                                                      {Tessage.GeneratedId}         bigint IDENTITY(1,1) NOT NULL,
                                                                      {Tessage.TypeId}              uniqueidentifier     NOT NULL,
                                                                      {Tessage.TessageId}           uniqueidentifier     NOT NULL,
                                                                      {Tessage.Status}              smallint             NOT NULL,
                                                                      {Tessage.Body}                nvarchar(MAX)        NOT NULL,
                                                                      {Tessage.ExceptionCount}      int                  NOT NULL  DEFAULT 0,
                                                                      {Tessage.ExceptionType}       nvarchar(500)        NULL,
                                                                      {Tessage.ExceptionStackTrace} nvarchar(MAX)        NULL,
                                                                      {Tessage.ExceptionTessage}    nvarchar(MAX)        NULL,


                                                                      CONSTRAINT PK_{Tessage.TableName} PRIMARY KEY CLUSTERED ( [{Tessage.GeneratedId}] ASC ),

                                                                      CONSTRAINT IX_{Tessage.TableName}_Unique_{Tessage.TessageId} UNIQUE ( {Tessage.TessageId} )
                                                                  )
                                                              END

                                                              """).caf()).caf();
   }
}
