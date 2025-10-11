using Compze.Sql.PostgreSql.Infrastructure;
using Compze.Utilities.Threading.TasksCE;
using Message = Compze.Tessaging.Hosting.Implementation.IServiceBusSqlLayer.OutboxMessagesDatabaseSchemaStrings;
using Dispatch = Compze.Tessaging.Hosting.Implementation.IServiceBusSqlLayer.OutboxMessageDispatchingTableSchemaStrings;

namespace Compze.Tessaging.Sql.PostgreSql;

partial class PgSqlOutboxSqlLayer
{
   const string PgSqlGuidType = "UUID";
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


                                                                 """).caf();
      }
   }
}