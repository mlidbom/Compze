using Peers = Compze.Tessaging.Transport.SqlLayer.ITessagingSqlLayer.PeersDatabaseSchemaStrings;
using Types = Compze.Tessaging.Transport.SqlLayer.ITessagingSqlLayer.PeerHandledTessageTypesDatabaseSchemaStrings;

namespace Compze.Tessaging.MySql;

partial class MySqlPeerRegistrySqlLayer
{
   const string MySqlGuidType = "CHAR(36)";

   public const string SchemaCreationSql =
      $"""

        CREATE TABLE IF NOT EXISTS {Peers.TableName}
        (
            {Peers.EndpointId} {MySqlGuidType} NOT NULL,

            PRIMARY KEY ( {Peers.EndpointId} )
        )
        ENGINE = InnoDB
        DEFAULT CHARACTER SET = utf8mb4;

        CREATE TABLE IF NOT EXISTS {Types.TableName}
        (
            {Types.EndpointId}         {MySqlGuidType} NOT NULL,
            {Types.HandledTessageType} MEDIUMTEXT      NOT NULL,

            FOREIGN KEY ({Types.EndpointId}) REFERENCES {Peers.TableName} ({Peers.EndpointId})
        )
        ENGINE = InnoDB
        DEFAULT CHARACTER SET = utf8mb4;

       """;
}
