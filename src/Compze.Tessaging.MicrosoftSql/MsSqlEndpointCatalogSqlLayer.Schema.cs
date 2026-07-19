using Catalog = Compze.Tessaging.Transport.SqlLayer.ITessagingSqlLayer.EndpointCatalogDatabaseSchemaStrings;

namespace Compze.Tessaging.MicrosoftSql;

partial class MsSqlEndpointCatalogSqlLayer
{
   //Deliberately unprefixed: the endpoint catalog is the domain database's one shared Tessaging table.
   public const string SchemaCreationSql =
      $"""

       IF NOT EXISTS (select name from sys.tables where name = '{Catalog.TableName}')
       BEGIN
           CREATE TABLE {Catalog.TableName}
           (
               {Catalog.EndpointName}           nvarchar(64)     NOT NULL,
               {Catalog.EndpointId}             uniqueidentifier NOT NULL,
               {Catalog.CreatedUtc}             datetime2        NOT NULL,
               {Catalog.LeaseHolderId}          uniqueidentifier NULL,
               {Catalog.LeaseHolderDescription} nvarchar(500)    NULL,
               {Catalog.LeaseHeartbeatUtc}      datetime2        NULL,

               CONSTRAINT PK_{Catalog.TableName} PRIMARY KEY CLUSTERED ( {Catalog.EndpointName} ),

               CONSTRAINT IX_{Catalog.TableName}_Unique_{Catalog.EndpointId} UNIQUE ( {Catalog.EndpointId} )
           )
       END

       """;
}
