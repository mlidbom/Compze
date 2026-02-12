using Compze.Core.Public;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Tessaging.Internal.SqlLayer;
using Compze.Sql.Common;
using Compze.Utilities.Contracts;
using System.Threading.Tasks;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;
using TessageTable =  Compze.Core.Tessaging.Internal.SqlLayer.IServiceBusSqlLayer.InboxTessageDatabaseSchemaStrings;

namespace Compze.Sql.MySql.Private.Tessaging;

public partial class MySqlInboxSqlLayer(IMySqlConnectionPool connectionFactory, MySqlSqlLayerSchemaManager schemaManager) : IServiceBusSqlLayer.IInboxSqlLayer
{
   readonly IMySqlConnectionPool _connectionFactory = connectionFactory;
   readonly MySqlSqlLayerSchemaManager _schemaManager = schemaManager;

   public IServiceBusSqlLayer.SaveTessageResult SaveTessage(TessageId tessageId, TypeId typeId, string serializedTessage)
   {
      return _connectionFactory.UseCommand(
         command =>
         {
            var affectedRows = command
              .SetCommandText(
                  $"""

                   INSERT {TessageTable.TableName} 
                               ({TessageTable.TessageId},  {TessageTable.TypeId},  {TessageTable.Body}, {TessageTable.Status}) 
                       VALUES (@{TessageTable.TessageId}, @{TessageTable.TypeId}, @{TessageTable.Body}, {(int)InboxTessageStatus.UnHandled})
                   ON DUPLICATE KEY UPDATE {TessageTable.TessageId} = {TessageTable.TessageId}

                   """)
              .AddParameter(TessageTable.TessageId, tessageId.Value)
              .AddParameter(TessageTable.TypeId, typeId.Value)
               //performance: Like with the tevent store, keep all framework properties out of the JSON and put it into separate columns instead. For tevents. Reuse a pre-serialized instance from the persisting to the tevent store.
              .AddMediumTextParameter(TessageTable.Body, serializedTessage)
              .ExecuteNonQuery();

            return affectedRows == 1 
               ? IServiceBusSqlLayer.SaveTessageResult.NewTessage 
               : IServiceBusSqlLayer.SaveTessageResult.Duplicate;
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
                              .AddParameter(TessageTable.TessageId, tessageId.Value)
                              .ExecuteNonQuery();

            Assert.Result.Is(affectedRows == 1);
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
                   .AddParameter(TessageTable.TessageId, tessageId.Value)
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
                   .AddParameter(TessageTable.TessageId, tessageId.Value)
                   .ExecuteNonQuery());
   }

   public async Task InitAsync() => await _schemaManager.EnsureSchemaInitializedAsync().caf();
}