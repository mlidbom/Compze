using Compze.Abstractions.Public;
using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Core.Tessaging.Internal.SqlLayer;
using Compze.Internals.Sql.Common;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using TessageTable =  Compze.Core.Tessaging.Internal.SqlLayer.IServiceBusSqlLayer.InboxTessageDatabaseSchemaStrings;

namespace Compze.Internals.Sql.Sqlite.Private.Tessaging;

partial class SqliteInboxSqlLayer(ISqliteConnectionPool connectionFactory, SqliteSqlLayerSchemaManager schemaManager) : IServiceBusSqlLayer.IInboxSqlLayer
{
   readonly ISqliteConnectionPool _connectionFactory = connectionFactory;
   readonly SqliteSqlLayerSchemaManager _schemaManager = schemaManager;

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
                   ON CONFLICT ({TessageTable.TessageId}) DO NOTHING

                   """)
              .AddMediumTextParameter(TessageTable.TessageId, tessageId.ToString())
              .AddMediumTextParameter(TessageTable.TypeId, typeId.GuidValue.ToString())
              .AddMediumTextParameter(TessageTable.Body, serializedTessage)
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
                       AND {TessageTable.Status} = {(int)InboxTessageStatus.UnHandled}

                   """)
              .AddMediumTextParameter(TessageTable.TessageId, tessageId.ToString())
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
                                
                        WHERE {TessageTable.TessageId} = @{TessageTable.TessageId}

                        """)
                   .AddMediumTextParameter(TessageTable.TessageId, tessageId.ToString())
                   .AddMediumTextParameter(TessageTable.ExceptionStackTrace, exceptionStackTrace)
                   .AddMediumTextParameter(TessageTable.ExceptionTessage, exceptionTessage)
                   .AddMediumTextParameter(TessageTable.ExceptionType, exceptionType)
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
                            AND {TessageTable.Status} = {(int)InboxTessageStatus.UnHandled}
                        """)
                   .AddMediumTextParameter(TessageTable.TessageId, tessageId.ToString())
                   .ExecuteNonQuery());
   }

   public async Task InitAsync() => await _schemaManager.EnsureSchemaInitializedAsync().caf();
}
