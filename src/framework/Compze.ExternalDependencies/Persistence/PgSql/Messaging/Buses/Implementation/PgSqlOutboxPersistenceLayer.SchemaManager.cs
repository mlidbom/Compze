﻿using System.Threading.Tasks;
using Compze.Persistence.PgSql.SystemExtensions;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Message = Compze.Messaging.Buses.Implementation.IServiceBusPersistenceLayer.OutboxMessagesDatabaseSchemaStrings;
using Dispatch = Compze.Messaging.Buses.Implementation.IServiceBusPersistenceLayer.OutboxMessageDispatchingTableSchemaStrings;

namespace Compze.Persistence.PgSql.Messaging.Buses.Implementation;

partial class PgSqlOutboxPersistenceLayer
{
   const string PgSqlGuidType = "CHAR(36)";
   static class SchemaManager
   {
      public static async Task EnsureTablesExistAsync(IPgSqlConnectionPool connectionFactory)
      {


         await connectionFactory.PrepareAndExecuteNonQueryAsync($"""
                                                                 
                                                                    
                                                                 
                                                                     CREATE TABLE IF NOT EXISTS {Message.TableName}
                                                                     (
                                                                         {Message.GeneratedId}       bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
                                                                         {Message.TypeIdGuidValue}   {PgSqlGuidType}                     NOT NULL,
                                                                         {Message.MessageId}         {PgSqlGuidType}                     NOT NULL,
                                                                         {Message.SerializedMessage} TEXT                                NOT NULL,
                                                                 
                                                                         PRIMARY KEY ({Message.GeneratedId}),
                                                                 
                                                                         CONSTRAINT IX_{Message.TableName}_Unique_{Message.MessageId} UNIQUE ( {Message.MessageId} )
                                                                     );
                                                                 
                                                                 
                                                                     CREATE TABLE  IF NOT EXISTS {Dispatch.TableName}
                                                                     (
                                                                         {Dispatch.MessageId}  {PgSqlGuidType} NOT NULL,
                                                                         {Dispatch.EndpointId} {PgSqlGuidType} NOT NULL,
                                                                         {Dispatch.IsReceived} boolean         NOT NULL,
                                                                 
                                                                 
                                                                         PRIMARY KEY ( {Dispatch.MessageId}, {Dispatch.EndpointId}),
                                                                             
                                                                 
                                                                         FOREIGN KEY ({Dispatch.MessageId}) REFERENCES {Message.TableName} ({Message.MessageId})
                                                                     );


                                                                 """).CaF();
      }
   }
}