using Catalog = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.EndpointCatalogDatabaseSchemaStrings;

namespace Compze.Tessaging.Sqlite._private;

partial class SqliteEndpointCatalogSqlLayer
{
   //Deliberately unprefixed: the endpoint catalog is the domain database's one shared Tessaging table.
   //The datetime columns are INTEGER: sqlite datetimes are stored as UTC ticks (see AddDateTime2Parameter).
   public const string SchemaCreationSql =
      $"""

       CREATE TABLE IF NOT EXISTS {Catalog.TableName}
       (
           {Catalog.EndpointName}          TEXT    NOT NULL,
           {Catalog.EndpointId}            TEXT    NOT NULL UNIQUE,
           {Catalog.CreatedUtc}            INTEGER NOT NULL,
           {Catalog.LockHolderDescription} TEXT    NULL,

           PRIMARY KEY ( {Catalog.EndpointName} )
       );

       """;
}
