using System;
using System.Threading.Tasks;
using Compze.Sql.Common;
using Compze.Sql.MySql.SystemExtensions;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Utilities.Contracts;
using Compze.Utilities.Threading.TasksCE;
using TessageTable =  Compze.Tessaging.Hosting.Implementation.IServiceBusSqlLayer.InboxTessageDatabaseSchemaStrings;

namespace Compze.Tessaging.Sql.MySql;

internal partial class MySqlInboxSqlLayer(IMySqlConnectionPool connectionFactory) : IServiceBusSqlLayer.IInboxSqlLayer
{
   readonly IMySqlConnectionPool _connectionFactory = connectionFactory;

   public IServiceBusSqlLayer.SaveTessageResult SaveTessage(Guid tessageId, Guid typeId, string serializedTessage)
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
              .AddParameter(TessageTable.TessageId, tessageId)
              .AddParameter(TessageTable.TypeId, typeId)
               //performance: Like with the event store, keep all framework properties out of the JSON and put it into separate columns instead. For events. Reuse a pre-serialized instance from the persisting to the event store.
              .AddMediumTextParameter(TessageTable.Body, serializedTessage)
              .ExecuteNonQuery();

            return affectedRows == 1 
               ? IServiceBusSqlLayer.SaveTessageResult.NewTessage 
               : IServiceBusSqlLayer.SaveTessageResult.Duplicate;
         });
   }

   public void MarkAsSucceeded(Guid tessageId)
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
                              .AddParameter(TessageTable.TessageId, tessageId)
                              .ExecuteNonQuery();

            Assert.Result.Is(affectedRows == 1);
            return affectedRows;
         });
   }

   public int RecordException(Guid tessageId, string exceptionStackTrace, string exceptionTessage, string exceptionType)
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
                   .AddParameter(TessageTable.TessageId, tessageId)
                   .AddMediumTextParameter(TessageTable.ExceptionStackTrace, exceptionStackTrace)
                   .AddMediumTextParameter(TessageTable.ExceptionTessage, exceptionTessage)
                   .AddVarcharParameter(TessageTable.ExceptionType, 500, exceptionType)
                   .ExecuteNonQuery());
   }

   public int MarkAsFailed(Guid tessageId)
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
                   .AddParameter(TessageTable.TessageId, tessageId)
                   .ExecuteNonQuery());
   }

   public async Task InitAsync() => await SchemaManager.EnsureTablesExistAsync(_connectionFactory).caf();
}