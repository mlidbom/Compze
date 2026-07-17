using Compze.Abstractions.Hosting.Public;
using Compze.Internals.Sql.Common;
using Compze.Internals.Sql.MySql;
using Compze.Internals.Sql.MySql.Private;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Transport.SqlLayer;
using Peers = Compze.Tessaging.Transport.SqlLayer.IServiceBusSqlLayer.PeersDatabaseSchemaStrings;
using Types = Compze.Tessaging.Transport.SqlLayer.IServiceBusSqlLayer.PeerHandledTessageTypesDatabaseSchemaStrings;

namespace Compze.Tessaging.MySql;

partial class MySqlPeerRegistrySqlLayer(IMySqlConnectionPool connectionFactory, MySqlSqlLayerSchemaManager schemaManager) : IServiceBusSqlLayer.IPeerRegistrySqlLayer
{
   readonly IMySqlConnectionPool _connectionFactory = connectionFactory;
   readonly MySqlSqlLayerSchemaManager _schemaManager = schemaManager;

   public void SaveAdvertisement(EndpointId peerId, IReadOnlySet<string> handledTessageTypes)
   {
      _connectionFactory.UseCommand(
         command =>
         {
            command
              .SetCommandText(
                  $"""

                   INSERT IGNORE INTO {Peers.TableName} ({Peers.EndpointId}) VALUES (@{Peers.EndpointId});

                   DELETE FROM {Types.TableName} WHERE {Types.EndpointId} = @{Types.EndpointId};

                   """)
              .AddParameter(Peers.EndpointId, peerId.Value);

            handledTessageTypes.ForEach(
               (handledTessageType, index)
                  => command.AppendCommandText($"""

                                                INSERT INTO {Types.TableName}
                                                            ({Types.EndpointId},  {Types.HandledTessageType})
                                                    VALUES (@{Types.EndpointId}, @{Types.HandledTessageType}_{index});

                                                """).AddMediumTextParameter($"{Types.HandledTessageType}_{index}", handledTessageType));

            command.ExecuteNonQuery();
         });
   }

   public IReadOnlyList<IServiceBusSqlLayer.PersistedPeer> GetPeers()
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
                    .Select(peer => new IServiceBusSqlLayer.PersistedPeer(
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
