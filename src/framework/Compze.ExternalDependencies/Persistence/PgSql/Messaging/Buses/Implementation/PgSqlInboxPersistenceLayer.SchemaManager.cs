﻿using System.Threading.Tasks;
using Compze.Persistence.PgSql.SystemExtensions;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Message =  Compze.Messaging.Buses.Implementation.IServiceBusPersistenceLayer.InboxMessageDatabaseSchemaStrings;

namespace Compze.Persistence.PgSql.Messaging.Buses.Implementation;

partial class PgSqlInboxPersistenceLayer
{
   const string PgSqlGuidType = "CHAR(36)";
   static class SchemaManager
   {
      public static async Task EnsureTablesExistAsync(IPgSqlConnectionPool connectionFactory)
      {

         await connectionFactory.PrepareAndExecuteNonQueryAsync($"""
                                                                 
                                                                 
                                                                 
                                                                     CREATE TABLE IF NOT EXISTS {Message.TableName}
                                                                     (
                                                                         {Message.GeneratedId}           bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
                                                                         {Message.TypeId}                {PgSqlGuidType}                     NOT NULL,
                                                                         {Message.MessageId}             {PgSqlGuidType}                     NOT NULL,
                                                                         {Message.Status}                smallint                            NOT NULL,
                                                                         {Message.Body}                  text                                NOT NULL,
                                                                         {Message.ExceptionCount}        int                                 NOT NULL  DEFAULT 0,
                                                                         {Message.ExceptionType}         varchar(500)                        NULL,
                                                                         {Message.ExceptionStackTrace}   text                                NULL,
                                                                         {Message.ExceptionMessage}      text                                NULL,
                                                                 
                                                                 
                                                                         PRIMARY KEY ( {Message.GeneratedId} ),
                                                                 
                                                                         CONSTRAINT IX_{Message.TableName}_Unique_{Message.MessageId} UNIQUE ( {Message.MessageId} )
                                                                     );




                                                                 """).CaF();

      }
   }
}