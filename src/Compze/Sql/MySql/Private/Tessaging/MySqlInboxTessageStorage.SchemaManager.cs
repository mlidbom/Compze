using System.Threading.Tasks;
using Compze.Sql.MySql.Private.SystemExtensions;
using Compze.Utilities.Threading.TasksCE;
using T =  Compze.Core.Tessaging.Internal.SqlLayer.IServiceBusSqlLayer.InboxTessageDatabaseSchemaStrings;

namespace Compze.Sql.MySql.Private.Tessaging;

partial class MySqlInboxSqlLayer
{
   const string MySqlGuidType = "CHAR(36)";
   static class SchemaManager
   {
      public static async Task EnsureTablesExistAsync(IMySqlConnectionPool connectionFactory)
      {
         await  connectionFactory.ExecuteNonQueryAsync($"""
                                                        
                                                        
                                                        
                                                            CREATE TABLE IF NOT EXISTS {T.TableName}
                                                            (
                                                                {T.GeneratedId}         bigint          NOT NULL  AUTO_INCREMENT,
                                                                {T.TypeId}              {MySqlGuidType} NOT NULL,
                                                                {T.TessageId}           {MySqlGuidType} NOT NULL,
                                                                {T.Status}              smallint        NOT NULL,
                                                                {T.Body}                mediumtext      NOT NULL,
                                                                {T.ExceptionCount}      int             NOT NULL  DEFAULT 0,
                                                                {T.ExceptionType}       varchar(500)    NULL,
                                                                {T.ExceptionStackTrace} mediumtext      NULL,
                                                                {T.ExceptionTessage}    mediumtext      NULL,
                                                        
                                                        
                                                                PRIMARY KEY ( {T.GeneratedId} ),
                                                        
                                                                UNIQUE INDEX IX_{T.TableName}_Unique_{T.TessageId} ( {T.TessageId} )
                                                            )
                                                        ENGINE = InnoDB
                                                        DEFAULT CHARACTER SET = utf8mb4;


                                                        """).caf();
      }
   }
}