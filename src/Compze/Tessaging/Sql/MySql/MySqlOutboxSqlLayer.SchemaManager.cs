using Compze.Sql.MySql.Infrastructure.SystemExtensions;
using Compze.Utilities.Threading.TasksCE;
using M = Compze.Tessaging.Hosting.Implementation.IServiceBusSqlLayer.OutboxMessagesDatabaseSchemaStrings;
using D = Compze.Tessaging.Hosting.Implementation.IServiceBusSqlLayer.OutboxMessageDispatchingTableSchemaStrings;

namespace Compze.Tessaging.Sql.MySql;

partial class MySqlOutboxSqlLayer
{
   const string MySqlGuidType = "CHAR(36)";
   static class SchemaManager
   {
      public static async Task EnsureTablesExistAsync(IMySqlConnectionPool connectionFactory)
      {
         await connectionFactory.ExecuteNonQueryAsync($"""
                                                       
                                                           CREATE TABLE IF NOT EXISTS {M.TableName}
                                                           (
                                                               {M.GeneratedId}       bigint          NOT NULL  AUTO_INCREMENT,
                                                               {M.TypeIdGuidValue}   {MySqlGuidType} NOT NULL,
                                                               {M.MessageId}         {MySqlGuidType} NOT NULL,
                                                               {M.SerializedMessage} MEDIUMTEXT      NOT NULL,
                                                       
                                                               PRIMARY KEY ( {M.GeneratedId}),
                                                       
                                                               UNIQUE INDEX IX_{M.TableName}_Unique_{M.MessageId} ( {M.MessageId} )
                                                           )
                                                           ENGINE = InnoDB
                                                           DEFAULT CHARACTER SET = utf8mb4;
                                                       
                                                           CREATE TABLE  IF NOT EXISTS {D.TableName}
                                                           (
                                                               {D.MessageId}        {MySqlGuidType} NOT NULL,
                                                               {D.EndpointId}       {MySqlGuidType} NOT NULL,
                                                               {D.IsReceived}       bit             NOT NULL,
                                                               {D.RetryCount}       int             NOT NULL DEFAULT 0,
                                                               {D.LastAttemptTime}  datetime        NULL,
                                                               {D.FailureReason}    MEDIUMTEXT      NULL,
                                                       
                                                       
                                                               PRIMARY KEY ( {D.MessageId}, {D.EndpointId}),
                                                                   /*WITH (ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF) ON PRIMARY,*/
                                                       
                                                               FOREIGN KEY ({D.MessageId}) REFERENCES {M.TableName} ({M.MessageId})
                                                           )
                                                           ENGINE = InnoDB
                                                           DEFAULT CHARACTER SET = utf8mb4;


                                                       """).caf();
      }
   }
}