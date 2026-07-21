using Compze.DocumentDb.Internal.SqlLayer;
using Compze.DocumentDb.Exceptions;
using Compze.Internals.Sql.Common;
using Compze.Internals.Sql.PostgreSql;
using Compze.Internals.Sql.PostgreSql.Private;
using Compze.TypeIdentifiers;
using Compze.TypeIdentifiers.Interning;
using Compze.Internals.SystemCE;
using Npgsql;
using System.Diagnostics.CodeAnalysis;
using Schema = Compze.DocumentDb.Internal.SqlLayer.IDocumentDbSqlLayer.DocumentTableSchemaStrings;
using Compze.Internals.Sql.PostgreSql.Internal;

namespace Compze.DocumentDb.PostgreSql.Private;

partial class PgSqlDocumentDbSqlLayer(IPgSqlConnectionPool connectionPool, PgSqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) : IDocumentDbSqlLayer
{
   readonly IPgSqlConnectionPool _connectionPool = connectionPool;
   readonly PgSqlSqlLayerSchemaManager _schemaManager = schemaManager;
   readonly ITypeIdInterner _typeIdInterner = typeIdInterner;

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
            connection.UseCommand(
               command => command.SetCommandText($"UPDATE {Schema.TableName} SET {Schema.Value} = @{Schema.Value}, {Schema.Updated} = @{Schema.Updated} WHERE {Schema.Id} = @{Schema.Id} AND {Schema.ValueTypeId} = @{Schema.ValueTypeId}")
                                 .AddVarcharParameter(Schema.Id, 500, writeRow.Id)
                                 .AddTimestampWithTimeZone(Schema.Updated, writeRow.UpdateTime)
                                 .AddParameter(Schema.ValueTypeId, internedTypeId)
                                 .AddMediumTextParameter(Schema.Value, writeRow.SerializedDocument)
                                 .PrepareStatement()
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

      var rows = _connectionPool.UseCommand(
         command => command.SetCommandText($"""

                                            SELECT {Schema.Value}, {Schema.ValueTypeId} FROM {Schema.TableName}  {UseUpdateLock(useUpdateLock)}
                                            WHERE {Schema.Id}=@{Schema.Id} AND {Schema.ValueTypeId} = {internedTypeId}
                                            """)
                           .AddVarcharParameter(Schema.Id, 500, idString)
                           .PrepareStatement()
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
                   .AddVarcharParameter(Schema.Id, 500, row.Id)
                   .AddParameter(Schema.ValueTypeId, internedTypeId)
                   .AddTimestampWithTimeZone(Schema.Created, row.UpdateTime)
                   .AddTimestampWithTimeZone(Schema.Updated, row.UpdateTime)
                   .AddMediumTextParameter(Schema.Value, row.SerializedDocument)
                   .PrepareStatement()
                   .ExecuteNonQuery();
         });
      }
      catch(PostgresException e)when(SqlExceptions.PgSql.IsPrimaryKeyViolation(e))
      {
         throw new AttemptToSaveAlreadyPersistedValueException(row.Id, row.SerializedDocument);
      }
   }

   public int Remove(string idString, TypeId typeId)
   {
      EnsureInitialized();
      if(!_typeIdInterner.TryGetInternedId(typeId, out var internedTypeId))
         return 0;

      return _connectionPool.UseCommand(
         command =>
            command.SetCommandText($"DELETE FROM {Schema.TableName} WHERE {Schema.Id} = @{Schema.Id} AND {Schema.ValueTypeId} = {internedTypeId}")
                   .AddVarcharParameter(Schema.Id, 500, idString)
                   .PrepareStatement()
                   .ExecuteNonQuery());
   }

   public IEnumerable<Guid> GetAllIds(TypeId typeId)
   {
      EnsureInitialized();
      if(!_typeIdInterner.TryGetInternedId(typeId, out var internedTypeId))
         return [];

      return _connectionPool.UseCommand(
         command => command.SetCommandText($"SELECT {Schema.Id} FROM {Schema.TableName} WHERE {Schema.ValueTypeId} = {internedTypeId}")
                           .PrepareStatement()//Performance: Does this work in Npgsql when there are no parameters? Should we have parameters?
                           .ExecuteReaderAndSelect(reader => reader.GetGuidFromString(0)));
   }

   public IReadOnlyList<IDocumentDbSqlLayer.ReadRow> GetAll(IEnumerable<Guid> ids, TypeId typeId)
   {
      EnsureInitialized();
      if(!_typeIdInterner.TryGetInternedId(typeId, out var internedTypeId))
         return [];

      var rows = _connectionPool.UseCommand(
         command => command.SetCommandText($"""
                                            SELECT {Schema.Id}, {Schema.Value}, {Schema.ValueTypeId} FROM {Schema.TableName} WHERE {Schema.ValueTypeId} = {internedTypeId}
                                                                               AND {Schema.Id} IN('
                                            """ + ids.Select(id => id.ToString()).Join("','") + "')")
                           .PrepareStatement() //Performance: Does this work in Npgsql when there are no parameters? Should we have parameters?
                           .ExecuteReaderAndSelect(reader => (SerializedDocument: reader.GetString(1), InternedTypeId: reader.GetInt32(2))));
      return rows.Select(ToReadRow).ToList();
   }

   public IReadOnlyList<IDocumentDbSqlLayer.ReadRow> GetAll(TypeId typeId)
   {
      EnsureInitialized();
      if(!_typeIdInterner.TryGetInternedId(typeId, out var internedTypeId))
         return [];

      var rows = _connectionPool.UseCommand(
         command => command.SetCommandText($"SELECT {Schema.Id}, {Schema.Value}, {Schema.ValueTypeId} FROM {Schema.TableName} WHERE {Schema.ValueTypeId} = {internedTypeId}")
                           .PrepareStatement() //Performance: Does this work in Npgsql when there are no parameters? Should we have parameters?
                           .ExecuteReaderAndSelect(reader => (SerializedDocument: reader.GetString(1), InternedTypeId: reader.GetInt32(2))));
      return rows.Select(ToReadRow).ToList();
   }

   // Resolve the interned id back to its canonical type string after the reader has closed — never while a connection is held.
   IDocumentDbSqlLayer.ReadRow ToReadRow((string SerializedDocument, int InternedTypeId) row) =>
      new(_typeIdInterner.GetTypeId(row.InternedTypeId), row.SerializedDocument);

   // ReSharper disable once UnusedParameter.Local
   static string UseUpdateLock(bool _) => "";// useUpdateLock ? "With(UPDLOCK, ROWLOCK)" : "";

   void EnsureInitialized() => _schemaManager.EnsureSchemaInitialized();
}
