using Compze.Sql.MicrosoftSql;
using Compze.Utilities.Threading.TasksCE;
using Message = Compze.Tessaging.Hosting.Implementation.IServiceBusSqlLayer.OutboxMessagesDatabaseSchemaStrings;
using D = Compze.Tessaging.Hosting.Implementation.IServiceBusSqlLayer.OutboxMessageDispatchingTableSchemaStrings;

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

                                            IF NOT EXISTS (select name from sys.tables where name = '{Message.TableName}')
                                            BEGIN
                                                CREATE TABLE dbo.{Message.TableName}
                                                (
                                                    {Message.GeneratedId}       bigint IDENTITY(1,1) NOT NULL,
                                                    {Message.TypeIdGuidValue}   uniqueidentifier     NOT NULL,
                                                    {Message.MessageId}         uniqueidentifier     NOT NULL,
                                                    {Message.SerializedMessage} nvarchar(MAX)        NOT NULL,
                                            
                                                    CONSTRAINT PK_{Message.TableName} PRIMARY KEY CLUSTERED ( [{Message.GeneratedId}] ASC ),
                                            
                                                    CONSTRAINT IX_{Message.TableName}_Unique_{Message.MessageId} UNIQUE ( {Message.MessageId} )
                                                )
                                            
                                                CREATE TABLE dbo.{D.TableName}
                                                (
                                                    {D.MessageId}        uniqueidentifier NOT NULL,
                                                    {D.EndpointId}       uniqueidentifier NOT NULL,
                                                    {D.IsReceived}       bit              NOT NULL,
                                                    {D.RetryCount}       int              NOT NULL DEFAULT 0,
                                                    {D.LastAttemptTime}  datetime2        NULL,
                                                    {D.FailureReason}    nvarchar(MAX)    NULL,
                                            
                                                    CONSTRAINT PK_{D.TableName} PRIMARY KEY CLUSTERED( {D.MessageId}, {D.EndpointId})
                                                        WITH (ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF),
                                            
                                                    CONSTRAINT FK_{D.TableName}_{D.MessageId} FOREIGN KEY ( {D.MessageId} )  REFERENCES {Message.TableName} ({Message.MessageId})
                                                )
                                            END

                                            """).caf();
      }
   }
}
