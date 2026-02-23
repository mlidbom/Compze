using Compze.Core.Public;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Tessaging.Internal.SqlLayer;
using Compze.Sql.Common;
using Compze.Utilities.Contracts;
using System.Threading.Tasks;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;
using TessageTable =  Compze.Core.Tessaging.Internal.SqlLayer.IServiceBusSqlLayer.InboxTessageDatabaseSchemaStrings;

namespace Compze.Sql.Sqlite.Private.Tessaging;

public partial class SqliteInboxSqlLayer(ISqliteConnectionPool connectionFactory, SqliteSqlLayerSchemaManager schemaManager) : IServiceBusSqlLayer.IInboxSqlLayer
{
   readonly ISqliteConnectionPool _connectionFactory = connectionFactory;
   readonly SqliteSqlLayerSchemaManager _schemaManager = schemaManager;

   public IServiceBusSqlLayer.SaveTessageResult SaveTessage(TessageId tessageId, TypeId typeId, string serializedTessage)
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
              .AddVarcharParameter(TessageTable.TessageId, 36, tessageId.ToString())
              .AddVarcharParameter(TessageTable.TypeId, 36, typeId.ToString())
              .AddMediumTextParameter(TessageTable.Body, serializedTessage)
              .ExecuteNonQuery();

            return affectedRows == 0 
               ? IServiceBusSqlLayer.SaveTessageResult.Duplicate 
               : IServiceBusSqlLayer.SaveTessageResult.NewTessage;
         });
   }

   public void MarkAsSucceeded(TessageId tessageId)
   {
      _connectionFactory.UseCommand(
         command =>
         {
            var affectedRows = command
                              .SetCommandText(
                                  $"""

                                   UPDATE {TessageTable.TableName} 
                                       SET {TessageTable.Status} = {(int)InboxTessageStatus.Succeeded}
                                   WHERE {TessageTable.TessageId} = @{TessageTable.TessageId}
                                       AND {TessageTable.Status} = {(int)InboxTessageStatus.UnHandled}

                                   """)
                              .AddVarcharParameter(TessageTable.TessageId, 36, tessageId.ToString())
                              .ExecuteNonQuery();

            Assert.ReturnValue.Is(affectedRows == 1);
            return affectedRows;
         });
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
                   .AddVarcharParameter(TessageTable.TessageId, 36, tessageId.ToString())
                   .AddMediumTextParameter(TessageTable.ExceptionStackTrace, exceptionStackTrace)
                   .AddMediumTextParameter(TessageTable.ExceptionTessage, exceptionTessage)
                   .AddVarcharParameter(TessageTable.ExceptionType, 500, exceptionType)
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
                   .AddVarcharParameter(TessageTable.TessageId, 36, tessageId.ToString())
                   .ExecuteNonQuery());
   }

   public async Task InitAsync() => await _schemaManager.EnsureSchemaInitializedAsync().caf();
}
