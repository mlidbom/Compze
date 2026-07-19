using Compze.Internals.Sql.Common;
using Compze.Internals.Sql.Sqlite;
using Compze.Internals.Sql.Sqlite.Private;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Abstractions.Public;
using Compze.Tessaging.Transport.SqlLayer;
using Compze.TypeIdentifiers;
using Compze.TypeIdentifiers.Interning;
using TessageTable =  Compze.Tessaging.Transport.SqlLayer.ITessagingSqlLayer.InboxTessageDatabaseSchemaStrings;

namespace Compze.Tessaging.Sqlite;

partial class SqliteInboxSqlLayer(ISqliteConnectionPool connectionFactory, SqliteSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner, EndpointTableSet tables) : ITessagingSqlLayer.IInboxSqlLayer
{
   readonly ISqliteConnectionPool _connectionFactory = connectionFactory;
   readonly SqliteSqlLayerSchemaManager _schemaManager = schemaManager;
   readonly ITypeIdInterner _typeIdInterner = typeIdInterner;
   readonly EndpointTableSet _tables = tables;

   public async Task<ITessagingSqlLayer.SaveTessageResult> SaveTessageAsync(TessageId tessageId, TypeId typeId, string serializedTessage)
   {
      // Intern before opening a connection: interning may hit the database, and nesting a second connection
      // inside a held one deadlocks the pool.
      var internedTypeId = _typeIdInterner.GetOrInternId(typeId);
      return await _connectionFactory.UseCommandAsync(
         async command =>
         {
            var affectedRows = await command
              .SetCommandText(
                  $"""

                   INSERT INTO {_tables.InboxTessages}
                               ({TessageTable.TessageId},  {TessageTable.TypeId},  {TessageTable.Body}, {TessageTable.Status})
                       VALUES (@{TessageTable.TessageId}, @{TessageTable.TypeId}, @{TessageTable.Body}, {(int)InboxTessageStatus.UnHandled})
                   ON CONFLICT ({TessageTable.TessageId}) DO NOTHING

                   """)
              .AddMediumTextParameter(TessageTable.TessageId, tessageId.ToString())
              .AddParameter(TessageTable.TypeId, internedTypeId)
              .AddMediumTextParameter(TessageTable.Body, serializedTessage)
              .ExecuteNonQueryAsync().caf();

            return affectedRows == 0
               ? ITessagingSqlLayer.SaveTessageResult.Duplicate
               : ITessagingSqlLayer.SaveTessageResult.NewTessage;
         }).caf();
   }

   public async Task<int> MarkAsSucceededAsync(TessageId tessageId)
   {
      return await _connectionFactory.UseCommandAsync(
         async command =>
            await command
              .SetCommandText(
                  $"""

                   UPDATE {_tables.InboxTessages}
                       SET {TessageTable.Status} = {(int)InboxTessageStatus.Succeeded}
                   WHERE {TessageTable.TessageId} = @{TessageTable.TessageId}
                       AND {TessageTable.Status} = {(int)InboxTessageStatus.UnHandled}

                   """)
              .AddMediumTextParameter(TessageTable.TessageId, tessageId.ToString())
              .ExecuteNonQueryAsync().caf()).caf();
   }

   public async Task<int> RecordExceptionAsync(TessageId tessageId, string exceptionStackTrace, string exceptionTessage, string exceptionType)
   {
      return await _connectionFactory.UseCommandAsync(
         async command => await command
                   .SetCommandText(
                       $"""

                        UPDATE {_tables.InboxTessages}
                            SET {TessageTable.ExceptionCount} = {TessageTable.ExceptionCount} + 1,
                                {TessageTable.ExceptionType} = @{TessageTable.ExceptionType},
                                {TessageTable.ExceptionStackTrace} = @{TessageTable.ExceptionStackTrace},
                                {TessageTable.ExceptionTessage} = @{TessageTable.ExceptionTessage}

                        WHERE {TessageTable.TessageId} = @{TessageTable.TessageId}

                        """)
                   .AddMediumTextParameter(TessageTable.TessageId, tessageId.ToString())
                   .AddMediumTextParameter(TessageTable.ExceptionStackTrace, exceptionStackTrace)
                   .AddMediumTextParameter(TessageTable.ExceptionTessage, exceptionTessage)
                   .AddMediumTextParameter(TessageTable.ExceptionType, exceptionType)
                   .ExecuteNonQueryAsync().caf()).caf();
   }

   public async Task<int> MarkAsFailedAsync(TessageId tessageId)
   {
      return await _connectionFactory.UseCommandAsync(
         async command => await command
                   .SetCommandText(
                       $"""

                        UPDATE {_tables.InboxTessages}
                            SET {TessageTable.Status} = {(int)InboxTessageStatus.Failed}
                        WHERE {TessageTable.TessageId} = @{TessageTable.TessageId}
                            AND {TessageTable.Status} = {(int)InboxTessageStatus.UnHandled}
                        """)
                   .AddMediumTextParameter(TessageTable.TessageId, tessageId.ToString())
                   .ExecuteNonQueryAsync().caf()).caf();
   }

   public async Task InitAsync() => await _schemaManager.EnsureSchemaInitializedAsync().caf();
}
