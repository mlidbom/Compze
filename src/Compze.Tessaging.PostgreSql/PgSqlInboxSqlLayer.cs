using Compze.Internals.Sql.Common;
using Compze.Internals.Sql.PostgreSql;
using Compze.Internals.Sql.PostgreSql.Private;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Abstractions.Public;
using Compze.Tessaging.Transport.SqlLayer;
using Compze.TypeIdentifiers;
using Compze.TypeIdentifiers.Interning;
using TessageTable =  Compze.Tessaging.Transport.SqlLayer.ITessagingSqlLayer.InboxTessageDatabaseSchemaStrings;

namespace Compze.Tessaging.PostgreSql;

partial class PgSqlInboxSqlLayer(IPgSqlConnectionPool connectionFactory, PgSqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner, EndpointTableSet tables) : ITessagingSqlLayer.IInboxSqlLayer
{
   readonly IPgSqlConnectionPool _connectionFactory = connectionFactory;
   readonly PgSqlSqlLayerSchemaManager _schemaManager = schemaManager;
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
                   ON CONFLICT ({TessageTable.TessageId}) DO NOTHING;

                   """)
              .AddParameter(TessageTable.TessageId, tessageId.Value)
              .AddParameter(TessageTable.TypeId, internedTypeId)
               //performance: Like with the tevent store, keep all framework properties out of the JSON and put it into separate columns instead. For tevents. Reuse a pre-serialized instance from the persisting to the tevent store.
              .AddMediumTextParameter(TessageTable.Body, serializedTessage)
              .PrepareStatement()
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
                       AND {TessageTable.Status} = {(int)InboxTessageStatus.UnHandled};

                   """)
              .AddParameter(TessageTable.TessageId, tessageId.Value)
              .PrepareStatement()
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
                                
                        WHERE {TessageTable.TessageId} = @{TessageTable.TessageId};

                        """)
                   .AddParameter(TessageTable.TessageId, tessageId.Value)
                   .AddMediumTextParameter(TessageTable.ExceptionStackTrace, exceptionStackTrace)
                   .AddMediumTextParameter(TessageTable.ExceptionTessage, exceptionTessage)
                   .AddVarcharParameter(TessageTable.ExceptionType, 500, exceptionType)
                   .PrepareStatement()
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
                            AND {TessageTable.Status} = {(int)InboxTessageStatus.UnHandled};
                        """)
                   .AddParameter(TessageTable.TessageId, tessageId.Value)
                   .PrepareStatement()
                   .ExecuteNonQueryAsync().caf()).caf();
   }

   public async Task InitAsync() => await _schemaManager.EnsureSchemaInitializedAsync().caf();
}
