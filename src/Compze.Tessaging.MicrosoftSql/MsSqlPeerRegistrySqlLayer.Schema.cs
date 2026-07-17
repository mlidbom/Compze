using Peers = Compze.Tessaging.Transport.SqlLayer.ITessagingSqlLayer.PeersDatabaseSchemaStrings;
using Types = Compze.Tessaging.Transport.SqlLayer.ITessagingSqlLayer.PeerHandledTessageTypesDatabaseSchemaStrings;

namespace Compze.Tessaging.MicrosoftSql;

partial class MsSqlPeerRegistrySqlLayer
{
   public const string SchemaCreationSql =
      $"""

       IF NOT EXISTS (select name from sys.tables where name = '{Peers.TableName}')
       BEGIN
           CREATE TABLE {Peers.TableName}
           (
               {Peers.EndpointId} uniqueidentifier NOT NULL,

               CONSTRAINT PK_{Peers.TableName} PRIMARY KEY CLUSTERED ( {Peers.EndpointId} )
           )

           CREATE TABLE {Types.TableName}
           (
               {Types.EndpointId}         uniqueidentifier NOT NULL,
               {Types.HandledTessageType} nvarchar(MAX)    NOT NULL,

               CONSTRAINT FK_{Types.TableName}_{Types.EndpointId} FOREIGN KEY ( {Types.EndpointId} ) REFERENCES {Peers.TableName} ({Peers.EndpointId})
           )

           CREATE INDEX IX_{Types.TableName}_{Types.EndpointId} ON {Types.TableName} ( {Types.EndpointId} )
       END

       """;
}
