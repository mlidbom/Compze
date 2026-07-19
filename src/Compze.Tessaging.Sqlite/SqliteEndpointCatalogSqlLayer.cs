using Compze.Tessaging.Endpoints;
using Compze.Internals.Sql.Common;
using Compze.Internals.Sql.Sqlite;
using Compze.Internals.Sql.Sqlite.Private;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Transport.SqlLayer;
using Microsoft.Data.Sqlite;
using Catalog = Compze.Tessaging.Transport.SqlLayer.ITessagingSqlLayer.EndpointCatalogDatabaseSchemaStrings;

namespace Compze.Tessaging.Sqlite;

partial class SqliteEndpointCatalogSqlLayer(ISqliteConnectionPool connectionFactory, SqliteSqlLayerSchemaManager schemaManager) : ITessagingSqlLayer.IEndpointCatalogSqlLayer
{
   readonly ISqliteConnectionPool _connectionFactory = connectionFactory;
   readonly SqliteSqlLayerSchemaManager _schemaManager = schemaManager;

   public async Task<ITessagingSqlLayer.EndpointCatalogEntry?> GetEntryByNameAsync(string endpointName) =>
      (await EntriesAsync($"WHERE {Catalog.EndpointName} = @filter", command => command.AddMediumTextParameter("filter", endpointName)).caf()).SingleOrDefault();

   public async Task<ITessagingSqlLayer.EndpointCatalogEntry?> GetEntryByEndpointIdAsync(EndpointId endpointId) =>
      (await EntriesAsync($"WHERE {Catalog.EndpointId} = @filter", command => command.AddMediumTextParameter("filter", endpointId.ToString())).caf()).SingleOrDefault();

   public async Task<IReadOnlyList<ITessagingSqlLayer.EndpointCatalogEntry>> GetEntriesAsync() =>
      await EntriesAsync(filterClause: "", addFilterParameter: _ => {}).caf();

   public async Task<bool> TryInsertEntryHoldingTheLeaseAsync(string endpointName, EndpointId endpointId, Guid leaseHolderId, string leaseHolderDescription, DateTime utcNow) =>
      await _connectionFactory.UseCommandAsync(
         async command => 1 == await command
                   .SetCommandText(
                       //Race-safe as it stands: sqlite serializes writers, so a racing registration's insert and this one's
                       //NOT EXISTS check cannot interleave.
                       $"""

                        INSERT INTO {Catalog.TableName}
                                    ({Catalog.EndpointName},  {Catalog.EndpointId},  {Catalog.CreatedUtc},  {Catalog.LeaseHolderId},  {Catalog.LeaseHolderDescription},  {Catalog.LeaseHeartbeatUtc})
                             SELECT @{Catalog.EndpointName}, @{Catalog.EndpointId}, @{Catalog.CreatedUtc}, @{Catalog.LeaseHolderId}, @{Catalog.LeaseHolderDescription}, @{Catalog.LeaseHeartbeatUtc}
                        WHERE NOT EXISTS (SELECT 1 FROM {Catalog.TableName} WHERE {Catalog.EndpointName} = @{Catalog.EndpointName})

                        """)
                   .AddMediumTextParameter(Catalog.EndpointName, endpointName)
                   .AddMediumTextParameter(Catalog.EndpointId, endpointId.ToString())
                   .AddDateTime2Parameter(Catalog.CreatedUtc, utcNow)
                   .AddMediumTextParameter(Catalog.LeaseHolderId, leaseHolderId.ToString())
                   .AddMediumTextParameter(Catalog.LeaseHolderDescription, leaseHolderDescription)
                   .AddDateTime2Parameter(Catalog.LeaseHeartbeatUtc, utcNow)
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
                   .AddMediumTextParameter(Catalog.LeaseHolderId, leaseHolderId.ToString())
                   .AddMediumTextParameter(Catalog.LeaseHolderDescription, leaseHolderDescription)
                   .AddDateTime2Parameter(Catalog.LeaseHeartbeatUtc, utcNow)
                   .AddDateTime2Parameter("StaleBefore", staleBefore)
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
                   .AddMediumTextParameter(Catalog.LeaseHolderId, leaseHolderId.ToString())
                   .AddDateTime2Parameter(Catalog.LeaseHeartbeatUtc, utcNow)
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
                   .AddMediumTextParameter(Catalog.LeaseHolderId, leaseHolderId.ToString())
                   .ExecuteNonQueryAsync().caf()).caf();

   async Task<IReadOnlyList<ITessagingSqlLayer.EndpointCatalogEntry>> EntriesAsync(string filterClause, Action<SqliteCommand> addFilterParameter) =>
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
                              endpointId: new EndpointId(reader.GetGuidFromString(1)),
                              createdUtc: new DateTime(reader.GetInt64(2), DateTimeKind.Utc),
                              leaseHolderDescription: await reader.IsDBNullAsync(3).caf() ? null : reader.GetString(3),
                              leaseHeartbeatUtc: await reader.IsDBNullAsync(4).caf() ? null : new DateTime(reader.GetInt64(4), DateTimeKind.Utc)));
            }

            return entries;
         }).caf();

   public async Task InitAsync() => await _schemaManager.EnsureSchemaInitializedAsync().caf();
}
