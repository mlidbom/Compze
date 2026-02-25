using Compze.Core.Public;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Tessaging.Internal.SqlLayer;
using Compze.Sql.Common;
using Compze.Contracts;
using System.Threading.Tasks;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;
using TessageTable = Compze.Core.Tessaging.Internal.SqlLayer.IServiceBusSqlLayer.InboxTessageDatabaseSchemaStrings;

namespace Compze.Sql.MicrosoftSql.Private.Tessaging;

public partial class MsSqlInboxSqlLayer(IMsSqlConnectionPool connectionFactory, MsSqlSqlLayerSchemaManager schemaManager) : IServiceBusSqlLayer.IInboxSqlLayer
{
   readonly IMsSqlConnectionPool _connectionFactory = connectionFactory;
   readonly MsSqlSqlLayerSchemaManager _schemaManager = schemaManager;

   public IServiceBusSqlLayer.SaveTessageResult SaveTessage(TessageId tessageId, TypeId typeId, string serializedTessage)
   {
      return _connectionFactory.UseCommand(command =>
      {
         var affectedRows = command
                           .SetCommandText(
                               $"""
                                MERGE {TessageTable.TableName} AS target
                                USING (SELECT @{TessageTable.TessageId} AS {TessageTable.TessageId}, --create a one row table "source" to be merged if its rows are not already in the table
                                              @{TessageTable.TypeId} AS {TessageTable.TypeId}, 
                                              @{TessageTable.Body} AS {TessageTable.Body}) AS source
                                ON target.{TessageTable.TessageId} = source.{TessageTable.TessageId}
                                WHEN NOT MATCHED THEN
                                    INSERT ({TessageTable.TessageId}, {TessageTable.TypeId}, {TessageTable.Body}, {TessageTable.Status})
                                    VALUES (source.{TessageTable.TessageId}, source.{TessageTable.TypeId}, source.{TessageTable.Body}, {(int)InboxTessageStatus.UnHandled});

                                """)
                           .AddParameter(TessageTable.TessageId, tessageId.Value)
                           .AddParameter(TessageTable.TypeId, typeId.Value)
                            //performance: Like with the tevent store, keep all framework properties out of the JSON and put it into separate columns instead. For tevents. Reuse a pre-serialized instance from the persisting to the tevent store.
                           .AddNVarcharMaxParameter(TessageTable.Body, serializedTessage)
                           .ExecuteNonQuery();

         return affectedRows == 0
                   ? IServiceBusSqlLayer.SaveTessageResult.Duplicate
                   : IServiceBusSqlLayer.SaveTessageResult.NewTessage;
      });
   }

   public void MarkAsSucceeded(TessageId tessageId)
   {
      _connectionFactory.UseCommand(command =>
      {
         command
           .SetCommandText(
               $"""

                UPDATE {TessageTable.TableName} 
                    SET {TessageTable.Status} = {(int)InboxTessageStatus.Succeeded}
                WHERE {TessageTable.TessageId} = @{TessageTable.TessageId}
                    AND {TessageTable.Status} = {(int)InboxTessageStatus.UnHandled}

                """)
           .AddParameter(TessageTable.TessageId, tessageId.Value)
           .ExecuteNonQuery()
           ._assert(affectedRows => affectedRows == 1);
      });
   }

   public int RecordException(TessageId tessageId, string exceptionStackTrace, string exceptionTessage, string exceptionType)
   {
      return _connectionFactory.UseCommand(command => command
                                                     .SetCommandText(
                                                         $"""

                                                          UPDATE {TessageTable.TableName} 
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
                                                     .ExecuteNonQuery());
   }

   public int MarkAsFailed(TessageId tessageId)
   {
      return _connectionFactory.UseCommand(command => command
                                                     .SetCommandText(
                                                         $"""

                                                          UPDATE {TessageTable.TableName} 
                                                              SET {TessageTable.Status} = {(int)InboxTessageStatus.Failed}
                                                          WHERE {TessageTable.TessageId} = @{TessageTable.TessageId}
                                                              AND {TessageTable.Status} = {(int)InboxTessageStatus.UnHandled}
                                                          """)
                                                     .AddParameter(TessageTable.TessageId, tessageId.Value)
                                                     .ExecuteNonQuery());
   }

   public async Task InitAsync() => await _schemaManager.EnsureSchemaInitializedAsync().caf();
}
