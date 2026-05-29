using Compze.DocumentDb.Internal.SqlLayer;
using Compze.DocumentDb.Internal.SqlLayer.Exceptions;
using Compze.Internals.Sql.Common;
using Compze.Internals.Sql.Common.Abstractions;
using Compze.TypeIdentifiers;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Underscore;
using Compze.Internals.SystemCE;
using Microsoft.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using Schema = Compze.DocumentDb.Internal.SqlLayer.IDocumentDbSqlLayer.DocumentTableSchemaStrings;

namespace Compze.Internals.Sql.MicrosoftSql.Private.DocumentDb;

partial class MsSqlDocumentDbSqlLayer : IDocumentDbSqlLayer
{
   public static IComponentRegistrar RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<IDocumentDbSqlLayer>()
                  .CreatedBy((IMsSqlConnectionPool connectionProvider, MsSqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new MsSqlDocumentDbSqlLayer(connectionProvider, schemaManager, typeIdInterner)));

   readonly IMsSqlConnectionPool _connectionPool;
   readonly MsSqlSqlLayerSchemaManager _schemaManager;
   readonly ITypeIdInterner _typeIdInterner;

   MsSqlDocumentDbSqlLayer(IMsSqlConnectionPool connectionPool, MsSqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner)
   {
      _schemaManager = schemaManager;
      _connectionPool = connectionPool;
      _typeIdInterner = typeIdInterner;
   }

   public void Update(IReadOnlyList<IDocumentDbSqlLayer.WriteRow> toUpdate)
   {
      EnsureInitialized();
      // Intern before opening a connection: interning may hit the database, and nesting a second connection
      // inside a held one deadlocks the pool.
      var rows = toUpdate.Select(writeRow => (writeRow, internedTypeId: _typeIdInterner.GetOrInternId(writeRow.TypeId))).ToList();
      _connectionPool.UseConnection(connection =>
      {
         foreach(var (writeRow, internedTypeId) in rows)
         {
            connection.UseCommand(command => command.SetCommandText($"UPDATE {Schema.TableName} SET {Schema.Value} = @{Schema.Value}, {Schema.Updated} = @{Schema.Updated} WHERE {Schema.Id} = @{Schema.Id} AND {Schema.ValueTypeId} = @{Schema.ValueTypeId}")
                                                    .AddNVarcharParameter(Schema.Id, 500, writeRow.Id)
                                                    .AddDateTime2Parameter(Schema.Updated, writeRow.UpdateTime)
                                                    .AddParameter(Schema.ValueTypeId, internedTypeId)
                                                    .AddNVarcharMaxParameter(Schema.Value, writeRow.SerializedDocument)
                                                    .ExecuteNonQuery());
         }
      });
   }

   public bool TryGet(string idString, TypeId typeId, bool useUpdateLock, [NotNullWhen(true)] out IDocumentDbSqlLayer.ReadRow? document)
   {
      EnsureInitialized();

      if(!_typeIdInterner.TryGetInternedId(typeId, out var internedTypeId))
      {
         document = null;
         return false;
      }

      var rows = _connectionPool.UseCommand(command => command.SetCommandText($"""

                                                                              SELECT {Schema.Value}, {Schema.ValueTypeId} FROM {Schema.TableName} {UseUpdateLock(useUpdateLock)}
                                                                              WHERE {Schema.Id}=@{Schema.Id} AND {Schema.ValueTypeId} = {internedTypeId}
                                                                              """)
                                                                   .AddNVarcharParameter(Schema.Id, 500, idString)
                                                                   .ExecuteReaderAndSelect(reader => (SerializedDocument: reader.GetString(0), InternedTypeId: reader.GetInt32(1))));
      if(rows.Count < 1)
      {
         document = null;
         return false;
      }

      document = ToReadRow(rows[0]);
      return true;
   }

   public void Add(IDocumentDbSqlLayer.WriteRow row)
   {
      EnsureInitialized();
      var internedTypeId = _typeIdInterner.GetOrInternId(row.TypeId);
      try
      {
         _connectionPool.UseCommand(command =>
         {
            command.SetCommandText($"INSERT INTO {Schema.TableName}({Schema.Id}, {Schema.ValueTypeId}, {Schema.Value}, {Schema.Created}, {Schema.Updated}) VALUES(@{Schema.Id}, @{Schema.ValueTypeId}, @{Schema.Value}, @{Schema.Created}, @{Schema.Updated})")
                   .AddNVarcharParameter(Schema.Id, 500, row.Id)
                   .AddParameter(Schema.ValueTypeId, internedTypeId)
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

   public int Remove(string idString, TypeId typeId)
   {
      EnsureInitialized();
      if(!_typeIdInterner.TryGetInternedId(typeId, out var internedTypeId))
         return 0;

      return _connectionPool.UseCommand(command =>
                                           command.SetCommandText($"DELETE FROM {Schema.TableName} WHERE {Schema.Id} = @{Schema.Id} AND {Schema.ValueTypeId} = {internedTypeId}")
                                                  .AddNVarcharParameter(Schema.Id, 500, idString)
                                                  .ExecuteNonQuery());
   }

   public IEnumerable<Guid> GetAllIds(TypeId typeId)
   {
      EnsureInitialized();
      if(!_typeIdInterner.TryGetInternedId(typeId, out var internedTypeId))
         return [];

      return _connectionPool.UseCommand(command => command.SetCommandText($"SELECT {Schema.Id} FROM {Schema.TableName} WHERE {Schema.ValueTypeId} = {internedTypeId}")
                                                          .ExecuteReaderAndSelect(reader => reader.GetGuidFromString(0)));
   }

   public IReadOnlyList<IDocumentDbSqlLayer.ReadRow> GetAll(IEnumerable<Guid> ids, TypeId typeId)
   {
      EnsureInitialized();
      if(!_typeIdInterner.TryGetInternedId(typeId, out var internedTypeId))
         return [];

      var rows = _connectionPool.UseCommand(command => command.SetCommandText($"""
                                                                              SELECT {Schema.Id}, {Schema.Value}, {Schema.ValueTypeId} FROM {Schema.TableName} WHERE {Schema.ValueTypeId} = {internedTypeId}
                                                                                                                 AND {Schema.Id} IN('
                                                                              """ + ids.Select(id => id.ToString()).Join("','") + "')")
                                                          .ExecuteReaderAndSelect(reader => (SerializedDocument: reader.GetString(1), InternedTypeId: reader.GetInt32(2))));
      return rows.Select(ToReadRow).ToList();
   }

   public IReadOnlyList<IDocumentDbSqlLayer.ReadRow> GetAll(TypeId typeId)
   {
      EnsureInitialized();
      if(!_typeIdInterner.TryGetInternedId(typeId, out var internedTypeId))
         return [];

      var rows = _connectionPool.UseCommand(command => command.SetCommandText($"SELECT {Schema.Id}, {Schema.Value}, {Schema.ValueTypeId} FROM {Schema.TableName} WHERE {Schema.ValueTypeId} = {internedTypeId}")
                                                          .ExecuteReaderAndSelect(reader => (SerializedDocument: reader.GetString(1), InternedTypeId: reader.GetInt32(2))));
      return rows.Select(ToReadRow).ToList();
   }

   // Resolve the interned id back to its canonical type string after the reader has closed — never while a connection is held.
   IDocumentDbSqlLayer.ReadRow ToReadRow((string SerializedDocument, int InternedTypeId) row) =>
      new(_typeIdInterner.GetTypeId(row.InternedTypeId), row.SerializedDocument);

   static string UseUpdateLock(bool useUpdateLock) => useUpdateLock ? "With(UPDLOCK, ROWLOCK)" : "";

   void EnsureInitialized() => _schemaManager.EnsureSchemaInitialized();
}
