using Compze.Tessaging.Endpoints;
using Compze.Internals.Sql.Common;
using Compze.Internals.Sql.PostgreSql;
using Compze.Internals.Sql.PostgreSql.Private;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Internal.SqlLayer;
using Npgsql;
using Catalog = Compze.Tessaging.Internal.SqlLayer.ITessagingSqlLayer.EndpointCatalogDatabaseSchemaStrings;

namespace Compze.Tessaging.PostgreSql.Private;

partial class PgSqlEndpointCatalogSqlLayer(IPgSqlConnectionPool connectionFactory, PgSqlSqlLayerSchemaManager schemaManager) : ITessagingSqlLayer.IEndpointCatalogSqlLayer
{
   readonly IPgSqlConnectionPool _connectionFactory = connectionFactory;
   readonly PgSqlSqlLayerSchemaManager _schemaManager = schemaManager;

   public async Task<ITessagingSqlLayer.EndpointCatalogEntry?> GetEntryByNameAsync(string endpointName) =>
      (await EntriesAsync($"WHERE {Catalog.EndpointName} = @filter", command => command.AddMediumTextParameter("filter", endpointName)).caf()).SingleOrDefault();

   public async Task<ITessagingSqlLayer.EndpointCatalogEntry?> GetEntryByEndpointIdAsync(EndpointId endpointId) =>
      (await EntriesAsync($"WHERE {Catalog.EndpointId} = @filter", command => command.AddParameter("filter", endpointId.Value)).caf()).SingleOrDefault();

   public async Task<IReadOnlyList<ITessagingSqlLayer.EndpointCatalogEntry>> GetEntriesAsync() =>
      await EntriesAsync(filterClause: "", addFilterParameter: _ => {}).caf();

   public async Task<bool> TryInsertEntryHoldingTheLeaseAsync(string endpointName, EndpointId endpointId, Guid leaseHolderId, string leaseHolderDescription, DateTime utcNow) =>
      await _connectionFactory.UseCommandAsync(
         async command => 1 == await command
                   .SetCommandText(
                       //ON CONFLICT DO NOTHING targets the name key precisely: a racing registration's row makes this one
                       //affect zero rows, while an EndpointId-uniqueness violation still fails loud.
                       $"""

                        INSERT INTO {Catalog.TableName}
                                    ({Catalog.EndpointName},  {Catalog.EndpointId},  {Catalog.CreatedUtc},  {Catalog.LeaseHolderId},  {Catalog.LeaseHolderDescription},  {Catalog.LeaseHeartbeatUtc})
                            VALUES (@{Catalog.EndpointName}, @{Catalog.EndpointId}, @{Catalog.CreatedUtc}, @{Catalog.LeaseHolderId}, @{Catalog.LeaseHolderDescription}, @{Catalog.LeaseHeartbeatUtc})
                        ON CONFLICT ({Catalog.EndpointName}) DO NOTHING

                        """)
                   .AddMediumTextParameter(Catalog.EndpointName, endpointName)
                   .AddParameter(Catalog.EndpointId, endpointId.Value)
                   .AddTimestampWithTimeZone(Catalog.CreatedUtc, utcNow)
                   .AddParameter(Catalog.LeaseHolderId, leaseHolderId)
                   .AddMediumTextParameter(Catalog.LeaseHolderDescription, leaseHolderDescription)
                   .AddTimestampWithTimeZone(Catalog.LeaseHeartbeatUtc, utcNow)
                   .ExecuteNonQueryAsync().caf()).caf();

   public async Task<bool> TryTakeTheLeaseAsync(string endpointName, Guid leaseHolderId, string leaseHolderDescription, DateTime utcNow, DateTime staleBefore) =>
      await _connectionFactory.UseCommandAsync(
         async command => 1 == await command
                   .SetCommandText(
                       $"""

                        UPDATE {Catalog.TableName}
                            SET {Catalog.LeaseHolderId} = @{Catalog.LeaseHolderId},
                                {Catalog.LeaseHolderDescription} = @{Catalog.LeaseHolderDescription},
                                {Catalog.LeaseHeartbeatUtc} = @{Catalog.LeaseHeartbeatUtc}
                        WHERE {Catalog.EndpointName} = @{Catalog.EndpointName}
                          AND ({Catalog.LeaseHolderId} IS NULL OR {Catalog.LeaseHeartbeatUtc} < @StaleBefore)

                        """)
                   .AddMediumTextParameter(Catalog.EndpointName, endpointName)
                   .AddParameter(Catalog.LeaseHolderId, leaseHolderId)
                   .AddMediumTextParameter(Catalog.LeaseHolderDescription, leaseHolderDescription)
                   .AddTimestampWithTimeZone(Catalog.LeaseHeartbeatUtc, utcNow)
                   .AddTimestampWithTimeZone("StaleBefore", staleBefore)
                   .ExecuteNonQueryAsync().caf()).caf();

   public async Task<bool> TryHeartbeatAsync(string endpointName, Guid leaseHolderId, DateTime utcNow) =>
      await _connectionFactory.UseCommandAsync(
         async command => 1 == await command
                   .SetCommandText(
                       $"""

                        UPDATE {Catalog.TableName}
                            SET {Catalog.LeaseHeartbeatUtc} = @{Catalog.LeaseHeartbeatUtc}
                        WHERE {Catalog.EndpointName} = @{Catalog.EndpointName}
                          AND {Catalog.LeaseHolderId} = @{Catalog.LeaseHolderId}

                        """)
                   .AddMediumTextParameter(Catalog.EndpointName, endpointName)
                   .AddParameter(Catalog.LeaseHolderId, leaseHolderId)
                   .AddTimestampWithTimeZone(Catalog.LeaseHeartbeatUtc, utcNow)
                   .ExecuteNonQueryAsync().caf()).caf();

   public async Task ReleaseTheLeaseAsync(string endpointName, Guid leaseHolderId) =>
      await _connectionFactory.UseCommandAsync(
         async command => await command
                   .SetCommandText(
                       $"""

                        UPDATE {Catalog.TableName}
                            SET {Catalog.LeaseHolderId} = NULL,
                                {Catalog.LeaseHolderDescription} = NULL,
                                {Catalog.LeaseHeartbeatUtc} = NULL
                        WHERE {Catalog.EndpointName} = @{Catalog.EndpointName}
                          AND {Catalog.LeaseHolderId} = @{Catalog.LeaseHolderId}

                        """)
                   .AddMediumTextParameter(Catalog.EndpointName, endpointName)
                   .AddParameter(Catalog.LeaseHolderId, leaseHolderId)
                   .ExecuteNonQueryAsync().caf()).caf();

   async Task<IReadOnlyList<ITessagingSqlLayer.EndpointCatalogEntry>> EntriesAsync(string filterClause, Action<NpgsqlCommand> addFilterParameter) =>
      await _connectionFactory.UseCommandAsync(
         async command =>
         {
            var entries = new List<ITessagingSqlLayer.EndpointCatalogEntry>();

            command.SetCommandText(
               $"""

                SELECT {Catalog.EndpointName}, {Catalog.EndpointId}, {Catalog.CreatedUtc}, {Catalog.LeaseHolderDescription}, {Catalog.LeaseHeartbeatUtc}
                FROM {Catalog.TableName} {filterClause}

                """);
            addFilterParameter(command);

            var reader = await command.ExecuteReaderAsync().caf();
            await using var _ = reader.caf();
            while(await reader.ReadAsync().caf())
            {
               entries.Add(new ITessagingSqlLayer.EndpointCatalogEntry(
                              endpointName: reader.GetString(0),
                              endpointId: new EndpointId(reader.GetGuid(1)),
                              createdUtc: DateTime.SpecifyKind(reader.GetDateTime(2), DateTimeKind.Utc),
                              leaseHolderDescription: await reader.IsDBNullAsync(3).caf() ? null : reader.GetString(3),
                              leaseHeartbeatUtc: await reader.IsDBNullAsync(4).caf() ? null : DateTime.SpecifyKind(reader.GetDateTime(4), DateTimeKind.Utc)));
            }

            return entries;
         }).caf();

   public async Task InitAsync() => await _schemaManager.EnsureSchemaInitializedAsync().caf();
}
