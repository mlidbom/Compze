using Compze.Abstractions.Hosting.Public;
using Compze.Internals.Sql.Common;
using Compze.Internals.Sql.MicrosoftSql;
using Compze.Internals.Sql.MicrosoftSql.Private;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Transport.SqlLayer;
using Peers = Compze.Tessaging.Transport.SqlLayer.ITessagingSqlLayer.PeersDatabaseSchemaStrings;
using Types = Compze.Tessaging.Transport.SqlLayer.ITessagingSqlLayer.PeerHandledTessageTypesDatabaseSchemaStrings;

namespace Compze.Tessaging.MicrosoftSql;

partial class MsSqlPeerRegistrySqlLayer(IMsSqlConnectionPool connectionFactory, MsSqlSqlLayerSchemaManager schemaManager) : ITessagingSqlLayer.IPeerRegistrySqlLayer
{
   readonly IMsSqlConnectionPool _connectionFactory = connectionFactory;
   readonly MsSqlSqlLayerSchemaManager _schemaManager = schemaManager;

   public void SaveAdvertisement(EndpointId peerId, IReadOnlySet<string> handledTessageTypes)
   {
      _connectionFactory.UseCommand(
         command =>
         {
            command
              .SetCommandText(
                  $"""

                   IF NOT EXISTS (SELECT 1 FROM {Peers.TableName} WHERE {Peers.EndpointId} = @{Peers.EndpointId})
                       INSERT INTO {Peers.TableName} ({Peers.EndpointId}) VALUES (@{Peers.EndpointId});

                   DELETE FROM {Types.TableName} WHERE {Types.EndpointId} = @{Types.EndpointId};

                   """)
              .AddParameter(Peers.EndpointId, peerId.Value);

            handledTessageTypes.ForEach(
               (handledTessageType, index)
                  => command.AppendCommandText($"""

                                                INSERT INTO {Types.TableName}
                                                            ({Types.EndpointId},  {Types.HandledTessageType})
                                                    VALUES (@{Types.EndpointId}, @{Types.HandledTessageType}_{index});

                                                """).AddNVarcharMaxParameter($"{Types.HandledTessageType}_{index}", handledTessageType));

            command.ExecuteNonQuery();
         });
   }

   public IReadOnlyList<ITessagingSqlLayer.PersistedPeer> GetPeers()
   {
      var rows = _connectionFactory.UseCommand(
         command =>
         {
            var raw = new List<(Guid EndpointId, string? HandledTessageType)>();

            command.SetCommandText(
               $"""

                SELECT p.{Peers.EndpointId}, t.{Types.HandledTessageType}
                FROM {Peers.TableName} p
                LEFT JOIN {Types.TableName} t ON p.{Peers.EndpointId} = t.{Types.EndpointId}

                """);

            using var reader = command.ExecuteReader();
            while(reader.Read())
            {
               raw.Add((reader.GetGuid(0), reader.IsDBNull(1) ? null : reader.GetString(1)));
            }

            return raw;
         });

      return [..rows.GroupBy(row => row.EndpointId)
                    .Select(peer => new ITessagingSqlLayer.PersistedPeer(
                               new EndpointId(peer.Key),
                               peer.Where(row => row.HandledTessageType != null).Select(row => row.HandledTessageType!).ToHashSet()))];
   }

   public void DeletePeer(EndpointId peerId)
   {
      _connectionFactory.UseCommand(
         command => command
                   .SetCommandText(
                       $"""

                        DELETE FROM {Types.TableName} WHERE {Types.EndpointId} = @{Types.EndpointId};
                        DELETE FROM {Peers.TableName} WHERE {Peers.EndpointId} = @{Peers.EndpointId};

                        """)
                   .AddParameter(Peers.EndpointId, peerId.Value)
                   .ExecuteNonQuery());
   }

   public async Task InitAsync() => await _schemaManager.EnsureSchemaInitializedAsync().caf();
}
