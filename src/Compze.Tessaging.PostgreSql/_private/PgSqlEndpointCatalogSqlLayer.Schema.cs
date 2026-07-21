using Catalog = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.EndpointCatalogDatabaseSchemaStrings;

namespace Compze.Tessaging.PostgreSql._private;

partial class PgSqlEndpointCatalogSqlLayer
{
   const string PgSqlGuidType = "UUID";

   //Deliberately unprefixed: the endpoint catalog is the domain database's one shared Tessaging table.
   public const string SchemaCreationSql =
      $"""

       CREATE TABLE IF NOT EXISTS {Catalog.TableName}
       (
         {Catalog.EndpointName}           TEXT            NOT NULL,
         {Catalog.EndpointId}             {PgSqlGuidType} NOT NULL,
         {Catalog.CreatedUtc}             timestamptz     NOT NULL,
         {Catalog.LeaseHolderId}          {PgSqlGuidType} NULL,
         {Catalog.LeaseHolderDescription} TEXT            NULL,
         {Catalog.LeaseHeartbeatUtc}      timestamptz     NULL,

         PRIMARY KEY ( {Catalog.EndpointName} ),

         CONSTRAINT IX_{Catalog.TableName}_Unique_{Catalog.EndpointId} UNIQUE ( {Catalog.EndpointId} )
       );

       """;
}
