using Peers = Compze.Tessaging.Transport.SqlLayer.IServiceBusSqlLayer.PeersDatabaseSchemaStrings;
using Types = Compze.Tessaging.Transport.SqlLayer.IServiceBusSqlLayer.PeerHandledTessageTypesDatabaseSchemaStrings;

namespace Compze.Tessaging.Sqlite;

partial class SqlitePeerRegistrySqlLayer
{
   public const string SchemaCreationSql =
      $"""

       CREATE TABLE IF NOT EXISTS {Peers.TableName}
       (
           {Peers.EndpointId} TEXT NOT NULL,

           PRIMARY KEY ( {Peers.EndpointId} )
       );

       CREATE TABLE IF NOT EXISTS {Types.TableName}
       (
           {Types.EndpointId}         TEXT NOT NULL,
           {Types.HandledTessageType} TEXT NOT NULL,

           FOREIGN KEY ( {Types.EndpointId} ) REFERENCES {Peers.TableName} ({Peers.EndpointId})
       );

       """;
}
