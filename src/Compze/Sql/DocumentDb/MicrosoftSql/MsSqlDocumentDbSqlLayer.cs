using System.Diagnostics.CodeAnalysis;
using Compze.Sql.Common;
using Compze.Sql.DocumentDb.Abstractions.Internal;
using Compze.Sql.MicrosoftSql.Infrastructure;
using Compze.Utilities.Contracts;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Threading.ResourceAccess;
using Microsoft.Data.SqlClient;
using Schema = Compze.Sql.DocumentDb.Abstractions.Internal.IDocumentDbSqlLayer.DocumentTableSchemaStrings;

namespace Compze.Sql.DocumentDb.MicrosoftSql;

internal partial class MsSqlDocumentDbSqlLayer : IDocumentDbSqlLayer
{
   readonly IMsSqlConnectionPool _connectionPool;
   readonly SchemaManager _schemaManager;
   bool _initialized;

   internal MsSqlDocumentDbSqlLayer(IMsSqlConnectionPool connectionPool)
   {
      _schemaManager = new SchemaManager(connectionPool);
      _connectionPool = connectionPool;
   }

   public void Update(IReadOnlyList<IDocumentDbSqlLayer.WriteRow> toUpdate)
   {
      EnsureInitialized();
      _connectionPool.UseConnection(connection =>
      {
         foreach(var writeRow in toUpdate)
         {
            connection.UseCommand(
               command => command.SetCommandText($"UPDATE {Schema.TableName} SET {Schema.Value} = @{Schema.Value}, {Schema.Updated} = @{Schema.Updated} WHERE {Schema.Id} = @{Schema.Id} AND {Schema.ValueTypeId} = @{Schema.ValueTypeId}")
                                 .AddNVarcharParameter(Schema.Id, 500, writeRow.Id)
                                 .AddDateTime2Parameter(Schema.Updated, writeRow.UpdateTime)
                                 .AddParameter(Schema.ValueTypeId, writeRow.TypeId)
                                 .AddNVarcharMaxParameter(Schema.Value, writeRow.SerializedDocument)
                                 .ExecuteNonQuery());
         }
      });
   }

   public bool TryGet(string idString, IReadOnlySet<Guid> acceptableTypeIds, bool useUpdateLock, [NotNullWhen(true)] out IDocumentDbSqlLayer.ReadRow? document)
   {
      EnsureInitialized();

      var documents = _connectionPool.UseCommand(
         command => command.SetCommandText($"""

                                            SELECT {Schema.Value}, {Schema.ValueTypeId} FROM {Schema.TableName} {UseUpdateLock(useUpdateLock)} 
                                            WHERE {Schema.Id}=@{Schema.Id} AND {Schema.ValueTypeId}  {TypeInClause(acceptableTypeIds)}
                                            """)
                           .AddNVarcharParameter(Schema.Id, 500, idString)
                           .ExecuteReaderAndSelect(reader => new IDocumentDbSqlLayer.ReadRow(reader.GetGuid(1), reader.GetString(0))));
      if(documents.Count < 1)
      {
         document = null;
         return false;
      }

      document = documents[0];

      return true;
   }

   public void Add(IDocumentDbSqlLayer.WriteRow row)
   {
      EnsureInitialized();
      try
      {
         _connectionPool.UseCommand(command =>
         {

            command.SetCommandText($"INSERT INTO {Schema.TableName}({Schema.Id}, {Schema.ValueTypeId}, {Schema.Value}, {Schema.Created}, {Schema.Updated}) VALUES(@{Schema.Id}, @{Schema.ValueTypeId}, @{Schema.Value}, @{Schema.Created}, @{Schema.Updated})")
                   .AddNVarcharParameter(Schema.Id, 500, row.Id)
                   .AddParameter(Schema.ValueTypeId, row.TypeId)
                   .AddDateTime2Parameter(Schema.Created, row.UpdateTime)
                   .AddDateTime2Parameter(Schema.Updated, row.UpdateTime)
                   .AddNVarcharMaxParameter(Schema.Value, row.SerializedDocument)
                   .ExecuteNonQuery();
         });
      }
      catch(SqlException exception)when(SqlExceptions.MsSql.IsPrimaryKeyViolation(exception))
      {
         throw new AttemptToSaveAlreadyPersistedValueException(row.Id, row.SerializedDocument);
      }
   }

   public int Remove(string idString, IReadOnlySet<Guid> acceptableTypes)
   {
      EnsureInitialized();
      return _connectionPool.UseCommand(
         command =>
            command.SetCommandText($"DELETE FROM {Schema.TableName} WHERE {Schema.Id} = @{Schema.Id} AND {Schema.ValueTypeId} {TypeInClause(acceptableTypes)}")
                   .AddNVarcharParameter(Schema.Id, 500, idString)
                   .ExecuteNonQuery());
   }

   public IEnumerable<Guid> GetAllIds(IReadOnlySet<Guid> acceptableTypes)
   {
      EnsureInitialized();
      return _connectionPool.UseCommand(
         command => command.SetCommandText($"SELECT {Schema.Id} FROM {Schema.TableName} WHERE {Schema.ValueTypeId} {TypeInClause(acceptableTypes)}")
                           .ExecuteReaderAndSelect(reader => Guid.Parse(reader.GetString(0)))); //bug: Huh, we store string but require them to be Guid!?
   }

   public IReadOnlyList<IDocumentDbSqlLayer.ReadRow> GetAll(IEnumerable<Guid> ids, IReadOnlySet<Guid> acceptableTypes)
   {
      EnsureInitialized();
      return _connectionPool.UseCommand(
         command => command.SetCommandText($"""
                                            SELECT {Schema.Id}, {Schema.Value}, {Schema.ValueTypeId} FROM {Schema.TableName} WHERE {Schema.ValueTypeId} {TypeInClause(acceptableTypes)} 
                                                                               AND {Schema.Id} IN('
                                            """ + ids.Select(id => id.ToString()).Join("','") + "')")
                           .ExecuteReaderAndSelect(reader => new IDocumentDbSqlLayer.ReadRow(reader.GetGuid(2), reader.GetString(1))));
   }

   public IReadOnlyList<IDocumentDbSqlLayer.ReadRow> GetAll(IReadOnlySet<Guid> acceptableTypes)
   {
      EnsureInitialized();
      return _connectionPool.UseCommand(
         command => command.SetCommandText($"SELECT {Schema.Id}, {Schema.Value}, {Schema.ValueTypeId} FROM {Schema.TableName} WHERE {Schema.ValueTypeId} {TypeInClause(acceptableTypes)}")
                           .ExecuteReaderAndSelect(reader => new IDocumentDbSqlLayer.ReadRow(reader.GetGuid(2), reader.GetString(1))));
   }

   static string TypeInClause(IReadOnlySet<Guid> acceptableTypeIds) => Assert.Argument.Is(acceptableTypeIds.Any()).then("IN( '" + acceptableTypeIds.Select(guid => guid.ToString()).Join("', '") + "')\n");

   static string UseUpdateLock(bool useUpdateLock) => useUpdateLock ? "With(UPDLOCK, ROWLOCK)" : "";

   readonly MonitorCE _monitor = MonitorCE.WithDefaultTimeout();
   void EnsureInitialized() => _monitor.Update(() =>
   {
      if(!_initialized)
      {
         _schemaManager.EnsureInitialized();
         _initialized = true;
      }
   });
}
