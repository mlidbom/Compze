using System;
using System.Threading.Tasks;
using Compze.Sql.Common;
using Compze.Sql.Sqlite;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Utilities.Contracts;
using Compze.Utilities.Threading.TasksCE;
using TessageTable =  Compze.Tessaging.Hosting.Implementation.IServiceBusSqlLayer.InboxTessageDatabaseSchemaStrings;

namespace Compze.Tessaging.Sql.Sqlite;

partial class SqliteInboxSqlLayer(ISqliteConnectionPool connectionFactory) : IServiceBusSqlLayer.IInboxSqlLayer
{
   readonly ISqliteConnectionPool _connectionFactory = connectionFactory;

   public IServiceBusSqlLayer.SaveTessageResult SaveTessage(Guid tessageId, Guid typeId, string serializedTessage)
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
                              .AddVarcharParameter(TessageTable.TessageId, 36, tessageId.ToString())
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
                   .AddVarcharParameter(TessageTable.TessageId, 36, tessageId.ToString())
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
                   .AddVarcharParameter(TessageTable.TessageId, 36, tessageId.ToString())
                   .ExecuteNonQuery());
   }

   public async Task InitAsync() => await SchemaManager.EnsureTablesExistAsync(_connectionFactory).caf();
}
