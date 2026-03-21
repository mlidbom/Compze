using Compze.Abstractions.Public;
using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Core.Tessaging.Internal.SqlLayer;
using Compze.Internals.Sql.Common;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using TessageTable =  Compze.Core.Tessaging.Internal.SqlLayer.IServiceBusSqlLayer.InboxTessageDatabaseSchemaStrings;

namespace Compze.Internals.Sql.PostgreSql.Private.Tessaging;

partial class PgSqlInboxSqlLayer(IPgSqlConnectionPool connectionFactory, PgSqlSqlLayerSchemaManager schemaManager) : IServiceBusSqlLayer.IInboxSqlLayer
{
   readonly IPgSqlConnectionPool _connectionFactory = connectionFactory;
   readonly PgSqlSqlLayerSchemaManager _schemaManager = schemaManager;

   public IServiceBusSqlLayer.SaveTessageResult SaveTessage(TessageId tessageId, MappedTypeId typeId, string serializedTessage)
   {
      return _connectionFactory.UseCommand(
         command =>
         {
            var affectedRows = command
              .SetCommandText(
                  $"""

                   INSERT INTO {TessageTable.TableName} 
                               ({TessageTable.TessageId},  {TessageTable.TypeId},  {TessageTable.Body}, {TessageTable.Status}) 
                       VALUES (@{TessageTable.TessageId}, @{TessageTable.TypeId}, @{TessageTable.Body}, {(int)InboxTessageStatus.UnHandled})
                   ON CONFLICT ({TessageTable.TessageId}) DO NOTHING;

                   """)
              .AddParameter(TessageTable.TessageId, tessageId.Value)
              .AddParameter(TessageTable.TypeId, typeId.GuidValue)
               //performance: Like with the tevent store, keep all framework properties out of the JSON and put it into separate columns instead. For tevents. Reuse a pre-serialized instance from the persisting to the tevent store.
              .AddMediumTextParameter(TessageTable.Body, serializedTessage)
              .PrepareStatement()
              .ExecuteNonQuery();

            return affectedRows == 0
               ? IServiceBusSqlLayer.SaveTessageResult.Duplicate
               : IServiceBusSqlLayer.SaveTessageResult.NewTessage;
         });
   }

   public int MarkAsSucceeded(TessageId tessageId)
   {
      return _connectionFactory.UseCommand(
         command =>
            command
              .SetCommandText(
                  $"""

                   UPDATE {TessageTable.TableName} 
                       SET {TessageTable.Status} = {(int)InboxTessageStatus.Succeeded}
                   WHERE {TessageTable.TessageId} = @{TessageTable.TessageId}
                       AND {TessageTable.Status} = {(int)InboxTessageStatus.UnHandled};

                   """)
              .AddParameter(TessageTable.TessageId, tessageId.Value)
              .PrepareStatement()
              .ExecuteNonQuery());
   }

   public int RecordException(TessageId tessageId, string exceptionStackTrace, string exceptionTessage, string exceptionType)
   {
      return _connectionFactory.UseCommand(
         command => command
                   .SetCommandText(
                       $"""

                        UPDATE {TessageTable.TableName} 
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
                   .ExecuteNonQuery());
   }

   public int MarkAsFailed(TessageId tessageId)
   {
      return _connectionFactory.UseCommand(
         command => command
                   .SetCommandText(
                       $"""

                        UPDATE {TessageTable.TableName} 
                            SET {TessageTable.Status} = {(int)InboxTessageStatus.Failed}
                        WHERE {TessageTable.TessageId} = @{TessageTable.TessageId}
                            AND {TessageTable.Status} = {(int)InboxTessageStatus.UnHandled};
                        """)
                   .AddParameter(TessageTable.TessageId, tessageId.Value)
                   .PrepareStatement()
                   .ExecuteNonQuery());
   }

   public async Task InitAsync() => await _schemaManager.EnsureSchemaInitializedAsync().caf();
}