using Compze.Tessaging.Transport.SqlLayer;
using Peers = Compze.Tessaging.Transport.SqlLayer.ITessagingSqlLayer.PeersDatabaseSchemaStrings;
using Types = Compze.Tessaging.Transport.SqlLayer.ITessagingSqlLayer.PeerHandledTessageTypesDatabaseSchemaStrings;

namespace Compze.Tessaging.MySql;

partial class MySqlPeerRegistrySqlLayer
{
   const string MySqlGuidType = "CHAR(36)";

   public static string SchemaCreationSql(EndpointTableSet tables) =>
      $"""

        CREATE TABLE IF NOT EXISTS {tables.Peers}
        (
            {Peers.EndpointId} {MySqlGuidType} NOT NULL,

            PRIMARY KEY ( {Peers.EndpointId} )
        )
        ENGINE = InnoDB
        DEFAULT CHARACTER SET = utf8mb4;

        CREATE TABLE IF NOT EXISTS {tables.PeerHandledTessageTypes}
        (
            {Types.EndpointId}         {MySqlGuidType} NOT NULL,
            {Types.HandledTessageType} MEDIUMTEXT      NOT NULL,

            FOREIGN KEY ({Types.EndpointId}) REFERENCES {tables.Peers} ({Peers.EndpointId})
        )
        ENGINE = InnoDB
        DEFAULT CHARACTER SET = utf8mb4;

       """;
}
