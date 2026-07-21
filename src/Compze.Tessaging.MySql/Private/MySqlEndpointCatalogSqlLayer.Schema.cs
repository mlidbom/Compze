using Catalog = Compze.Tessaging.Internal.SqlLayer.ITessagingSqlLayer.EndpointCatalogDatabaseSchemaStrings;

namespace Compze.Tessaging.MySql.Private;

partial class MySqlEndpointCatalogSqlLayer
{
   const string MySqlGuidType = "CHAR(36)";

   //Deliberately unprefixed: the endpoint catalog is the domain database's one shared Tessaging table.
   //The datetime columns are datetime(6): the lease heartbeats need sub-second precision.
   public const string SchemaCreationSql =
      $"""

        CREATE TABLE IF NOT EXISTS {Catalog.TableName}
        (
            {Catalog.EndpointName}           VARCHAR(64)     NOT NULL,
            {Catalog.EndpointId}             {MySqlGuidType} NOT NULL,
            {Catalog.CreatedUtc}             datetime(6)     NOT NULL,
            {Catalog.LeaseHolderId}          {MySqlGuidType} NULL,
            {Catalog.LeaseHolderDescription} VARCHAR(500)    NULL,
            {Catalog.LeaseHeartbeatUtc}      datetime(6)     NULL,

            PRIMARY KEY ( {Catalog.EndpointName} ),

            UNIQUE INDEX IX_{Catalog.TableName}_Unique_{Catalog.EndpointId} ( {Catalog.EndpointId} )
        )
        ENGINE = InnoDB
        DEFAULT CHARACTER SET = utf8mb4;

       """;
}
