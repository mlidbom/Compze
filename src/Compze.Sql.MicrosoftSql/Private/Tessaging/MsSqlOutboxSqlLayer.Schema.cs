using Outbox = Compze.Core.Tessaging.Internal.SqlLayer.IServiceBusSqlLayer.OutboxTessagesDatabaseSchemaStrings;
using Dispatching = Compze.Core.Tessaging.Internal.SqlLayer.IServiceBusSqlLayer.OutboxTessageDispatchingTableSchemaStrings;

namespace Compze.Sql.MicrosoftSql.Private.Tessaging;

public partial class MsSqlOutboxSqlLayer
{
   public const string SchemaCreationSql =
      $"""

       IF NOT EXISTS (select name from sys.tables where name = '{Outbox.TableName}')
       BEGIN
           CREATE TABLE {Outbox.TableName}
           (
               {Outbox.GeneratedId}       bigint IDENTITY(1,1) NOT NULL,
               {Outbox.TypeIdGuidValue}   uniqueidentifier     NOT NULL,
               {Outbox.TessageId}         uniqueidentifier     NOT NULL,
               {Outbox.SerializedTessage} nvarchar(MAX)        NOT NULL,

               CONSTRAINT PK_{Outbox.TableName} PRIMARY KEY CLUSTERED ( [{Outbox.GeneratedId}] ASC ),

               CONSTRAINT IX_{Outbox.TableName}_Unique_{Outbox.TessageId} UNIQUE ( {Outbox.TessageId} )
           )

           CREATE TABLE {Dispatching.TableName}
           (
               {Dispatching.TessageId}        uniqueidentifier NOT NULL,
               {Dispatching.EndpointId}       uniqueidentifier NOT NULL,
               {Dispatching.IsReceived}       bit              NOT NULL,
               {Dispatching.RetryCount}       int              NOT NULL DEFAULT 0,
               {Dispatching.LastAttemptTime}  datetime2        NULL,
               {Dispatching.FailureReason}    nvarchar(MAX)    NULL,

               CONSTRAINT PK_{Dispatching.TableName} PRIMARY KEY CLUSTERED( {Dispatching.TessageId}, {Dispatching.EndpointId})
                   WITH (ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF),

               CONSTRAINT FK_{Dispatching.TableName}_{Dispatching.TessageId} FOREIGN KEY ( {Dispatching.TessageId} )  REFERENCES {Outbox.TableName} ({Outbox.TessageId})
           )
       END

       """;
}
