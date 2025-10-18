using Compze.Sql.MicrosoftSql;
using Compze.Utilities.Threading.TasksCE;
using Outbox = Compze.Tessaging.Hosting.Implementation.IServiceBusSqlLayer.OutboxMessagesDatabaseSchemaStrings;
using Dispatching = Compze.Tessaging.Hosting.Implementation.IServiceBusSqlLayer.OutboxMessageDispatchingTableSchemaStrings;

namespace Compze.Tessaging.Sql.MicrosoftSql;

partial class MsSqlOutboxSqlLayer
{
   static class SchemaManager
   {
      //Performance: Why is the MessageId not the primary key? Are we worried about performance loss because of fragmentation because of non-sequential Guids? Is there a (performant and truly reliable) sequential-guid-generator we could use? How does it not being the clustered index impact row vs page etc locking?
      public static async Task EnsureTablesExistAsync(IMsSqlConnectionPool connectionFactory)
      {
         // ReSharper disable once MethodHasAsyncOverload | THis crashes with weird exception if called async so ...
         await connectionFactory.ExecuteNonQueryAsync($"""

                                            IF NOT EXISTS (select name from sys.tables where name = '{Outbox.TableName}')
                                            BEGIN
                                                CREATE TABLE {Outbox.TableName}
                                                (
                                                    {Outbox.GeneratedId}       bigint IDENTITY(1,1) NOT NULL,
                                                    {Outbox.TypeIdGuidValue}   uniqueidentifier     NOT NULL,
                                                    {Outbox.MessageId}         uniqueidentifier     NOT NULL,
                                                    {Outbox.SerializedMessage} nvarchar(MAX)        NOT NULL,
                                            
                                                    CONSTRAINT PK_{Outbox.TableName} PRIMARY KEY CLUSTERED ( [{Outbox.GeneratedId}] ASC ),
                                            
                                                    CONSTRAINT IX_{Outbox.TableName}_Unique_{Outbox.MessageId} UNIQUE ( {Outbox.MessageId} )
                                                )
                                            
                                                CREATE TABLE {Dispatching.TableName}
                                                (
                                                    {Dispatching.MessageId}        uniqueidentifier NOT NULL,
                                                    {Dispatching.EndpointId}       uniqueidentifier NOT NULL,
                                                    {Dispatching.IsReceived}       bit              NOT NULL,
                                                    {Dispatching.RetryCount}       int              NOT NULL DEFAULT 0,
                                                    {Dispatching.LastAttemptTime}  datetime2        NULL,
                                                    {Dispatching.FailureReason}    nvarchar(MAX)    NULL,
                                            
                                                    CONSTRAINT PK_{Dispatching.TableName} PRIMARY KEY CLUSTERED( {Dispatching.MessageId}, {Dispatching.EndpointId})
                                                        WITH (ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF),
                                            
                                                    CONSTRAINT FK_{Dispatching.TableName}_{Dispatching.MessageId} FOREIGN KEY ( {Dispatching.MessageId} )  REFERENCES {Outbox.TableName} ({Outbox.MessageId})
                                                )
                                            END

                                            """).caf();
      }
   }
}
