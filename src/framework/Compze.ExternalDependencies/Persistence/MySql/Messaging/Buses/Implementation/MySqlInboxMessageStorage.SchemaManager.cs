﻿using System.Threading.Tasks;
using Compze.Persistence.MySql.SystemExtensions;
using Compze.SystemCE.ThreadingCE.TasksCE;
using T =  Compze.Messaging.Buses.Implementation.IServiceBusPersistenceLayer.InboxMessageDatabaseSchemaStrings;

namespace Compze.Persistence.MySql.Messaging.Buses.Implementation;

partial class MySqlInboxPersistenceLayer
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
                                                                {T.MessageId}           {MySqlGuidType} NOT NULL,
                                                                {T.Status}              smallint        NOT NULL,
                                                                {T.Body}                mediumtext      NOT NULL,
                                                                {T.ExceptionCount}      int             NOT NULL  DEFAULT 0,
                                                                {T.ExceptionType}       varchar(500)    NULL,
                                                                {T.ExceptionStackTrace} mediumtext      NULL,
                                                                {T.ExceptionMessage}    mediumtext      NULL,
                                                        
                                                        
                                                                PRIMARY KEY ( {T.GeneratedId} ),
                                                        
                                                                UNIQUE INDEX IX_{T.TableName}_Unique_{T.MessageId} ( {T.MessageId} )
                                                            )
                                                        ENGINE = InnoDB
                                                        DEFAULT CHARACTER SET = utf8mb4;


                                                        """).CaF();
      }
   }
}