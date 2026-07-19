using Compze.Internals.Sql.Common;
using Compze.Internals.Sql.MicrosoftSql;
using Compze.Internals.Sql.MicrosoftSql.Private;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Transport.SqlLayer;
using Compze.TypeIdentifiers;
using Compze.TypeIdentifiers.Interning;
using TessageTable = Compze.Tessaging.Transport.SqlLayer.ITessagingSqlLayer.InboxTessageDatabaseSchemaStrings;

namespace Compze.Tessaging.MicrosoftSql;

partial class MsSqlInboxSqlLayer(IMsSqlConnectionPool connectionFactory, MsSqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner, EndpointTableSet tables) : ITessagingSqlLayer.IInboxSqlLayer
{
   readonly IMsSqlConnectionPool _connectionFactory = connectionFactory;
   readonly MsSqlSqlLayerSchemaManager _schemaManager = schemaManager;
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
                   MERGE {_tables.InboxTessages} AS target
                   USING (SELECT @{TessageTable.TessageId} AS {TessageTable.TessageId}, --create a one row table "source" to be merged if its rows are not already in the table
                                 @{TessageTable.TypeId} AS {TessageTable.TypeId},
                                 @{TessageTable.Body} AS {TessageTable.Body}) AS source
                   ON target.{TessageTable.TessageId} = source.{TessageTable.TessageId}
                   WHEN NOT MATCHED THEN
                       INSERT ({TessageTable.TessageId}, {TessageTable.TypeId}, {TessageTable.Body}, {TessageTable.Status})
                       VALUES (source.{TessageTable.TessageId}, source.{TessageTable.TypeId}, source.{TessageTable.Body}, {(int)InboxTessageStatus.UnHandled});

                   """)
              .AddParameter(TessageTable.TessageId, tessageId.Value)
              .AddParameter(TessageTable.TypeId, internedTypeId)
               //performance: Like with the tevent store, keep all framework properties out of the JSON and put it into separate columns instead. For tevents. Reuse a pre-serialized instance from the persisting to the tevent store.
              .AddNVarcharMaxParameter(TessageTable.Body, serializedTessage)
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
              .AddParameter(TessageTable.TessageId, tessageId.Value)
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
                   .AddParameter(TessageTable.TessageId, tessageId.Value)
                   .AddNVarcharMaxParameter(TessageTable.ExceptionStackTrace, exceptionStackTrace)
                   .AddNVarcharMaxParameter(TessageTable.ExceptionTessage, exceptionTessage)
                   .AddNVarcharParameter(TessageTable.ExceptionType, 500, exceptionType)
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
                   .AddParameter(TessageTable.TessageId, tessageId.Value)
                   .ExecuteNonQueryAsync().caf()).caf();
   }

   public async Task InitAsync() => await _schemaManager.EnsureSchemaInitializedAsync().caf();
}
