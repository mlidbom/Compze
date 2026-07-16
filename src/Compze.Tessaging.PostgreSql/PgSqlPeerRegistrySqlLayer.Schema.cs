using Peers = Compze.Tessaging.Transport.SqlLayer.IServiceBusSqlLayer.PeersDatabaseSchemaStrings;
using Types = Compze.Tessaging.Transport.SqlLayer.IServiceBusSqlLayer.PeerHandledTessageTypesDatabaseSchemaStrings;

namespace Compze.Tessaging.PostgreSql;

partial class PgSqlPeerRegistrySqlLayer
{
   const string PgSqlGuidType = "UUID";

   public const string SchemaCreationSql =
      $"""

       CREATE TABLE IF NOT EXISTS {Peers.TableName}
       (
         {Peers.EndpointId} {PgSqlGuidType} NOT NULL,

         PRIMARY KEY ( {Peers.EndpointId} )
       );

       CREATE TABLE IF NOT EXISTS {Types.TableName}
       (
        {Types.EndpointId}         {PgSqlGuidType} NOT NULL,
        {Types.HandledTessageType} TEXT            NOT NULL,

        FOREIGN KEY ({Types.EndpointId}) REFERENCES {Peers.TableName} ({Peers.EndpointId})
       );

       """;
}
