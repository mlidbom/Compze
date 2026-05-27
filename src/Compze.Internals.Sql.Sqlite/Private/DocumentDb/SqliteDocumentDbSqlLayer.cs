using Compze.DocumentDb.Internal.SqlLayer;
using Compze.DocumentDb.Internal.SqlLayer.Exceptions;
using Compze.Internals.Sql.Common;
using Compze.Internals.Sql.Common.Abstractions;
using Compze.Underscore;
using Compze.Internals.SystemCE;
using Microsoft.Data.Sqlite;
using System.Diagnostics.CodeAnalysis;
using Schema = Compze.DocumentDb.Internal.SqlLayer.IDocumentDbSqlLayer.DocumentTableSchemaStrings;

namespace Compze.Internals.Sql.Sqlite.Private.DocumentDb;

partial class SqliteDocumentDbSqlLayer(ISqliteConnectionPool connectionPool, SqliteSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) : IDocumentDbSqlLayer
{
   readonly ISqliteConnectionPool _connectionPool = connectionPool;
   readonly SqliteSqlLayerSchemaManager _schemaManager = schemaManager;
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
                                 .AddMediumTextParameter(Schema.Id, writeRow.Id)
                                 .AddDateTime2Parameter(Schema.Updated, writeRow.UpdateTime)
                                 .AddParameter(Schema.ValueTypeId, internedTypeId)
                                 .AddMediumTextParameter(Schema.Value, writeRow.SerializedDocument)
                                 .ExecuteNonQuery());
         }
      });
   }

   public bool TryGet(string idString, IReadOnlySet<string> acceptableTypeIds, bool useUpdateLock, [NotNullWhen(true)] out IDocumentDbSqlLayer.ReadRow? document)
   {
      EnsureInitialized();

      var acceptableInternedTypeIds = _typeIdInterner.GetExistingIds(acceptableTypeIds);
      if(acceptableInternedTypeIds.Count == 0)
      {
         document = null;
         return false;
      }

      var rows = _connectionPool.UseCommand(
         command => command.SetCommandText($"""

                                            SELECT {Schema.Value}, {Schema.ValueTypeId} FROM {Schema.TableName}
                                            WHERE {Schema.Id}=@{Schema.Id} AND {Schema.ValueTypeId} {TypeInClause(acceptableInternedTypeIds)}
                                            """)
                           .AddMediumTextParameter(Schema.Id, idString)
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
                   .AddMediumTextParameter(Schema.Id, row.Id)
                   .AddParameter(Schema.ValueTypeId, internedTypeId)
                   .AddDateTime2Parameter(Schema.Created, row.UpdateTime)
                   .AddDateTime2Parameter(Schema.Updated, row.UpdateTime)
                   .AddMediumTextParameter(Schema.Value, row.SerializedDocument)
                   .ExecuteNonQuery();
         });
      }
      catch(SqliteException exception)when(SqlExceptions.Sqlite.IsPrimaryKeyViolation(exception))
      {
         throw new AttemptToSaveAlreadyPersistedValueException(row.Id, row.SerializedDocument);
      }
   }

   public int Remove(string idString, IReadOnlySet<string> acceptableTypes)
   {
      EnsureInitialized();
      var acceptableInternedTypeIds = _typeIdInterner.GetExistingIds(acceptableTypes);
      if(acceptableInternedTypeIds.Count == 0)
         return 0;

      return _connectionPool.UseCommand(
         command =>
            command.SetCommandText($"DELETE FROM {Schema.TableName} WHERE {Schema.Id} = @{Schema.Id} AND {Schema.ValueTypeId} {TypeInClause(acceptableInternedTypeIds)}")
                   .AddMediumTextParameter(Schema.Id, idString)
                   .ExecuteNonQuery());
   }

   public IEnumerable<Guid> GetAllIds(IReadOnlySet<string> acceptableTypes)
   {
      EnsureInitialized();
      var acceptableInternedTypeIds = _typeIdInterner.GetExistingIds(acceptableTypes);
      if(acceptableInternedTypeIds.Count == 0)
         return [];

      return _connectionPool.UseCommand(
         command => command.SetCommandText($"SELECT {Schema.Id} FROM {Schema.TableName} WHERE {Schema.ValueTypeId} {TypeInClause(acceptableInternedTypeIds)}")
                           .ExecuteReaderAndSelect(reader => reader.GetGuidFromString(0)));
   }

   public IReadOnlyList<IDocumentDbSqlLayer.ReadRow> GetAll(IEnumerable<Guid> ids, IReadOnlySet<string> acceptableTypes)
   {
      EnsureInitialized();
      var acceptableInternedTypeIds = _typeIdInterner.GetExistingIds(acceptableTypes);
      if(acceptableInternedTypeIds.Count == 0)
         return [];

      var rows = _connectionPool.UseCommand(
         command => command.SetCommandText($"""
                                            SELECT {Schema.Id}, {Schema.Value}, {Schema.ValueTypeId} FROM {Schema.TableName} WHERE {Schema.ValueTypeId} {TypeInClause(acceptableInternedTypeIds)}
                                                                               AND {Schema.Id} IN('
                                            """ + ids.Select(id => id.ToString()).Join("','") + "')")
                           .ExecuteReaderAndSelect(reader => (SerializedDocument: reader.GetString(1), InternedTypeId: reader.GetInt32(2))));
      return rows.Select(ToReadRow).ToList();
   }

   public IReadOnlyList<IDocumentDbSqlLayer.ReadRow> GetAll(IReadOnlySet<string> acceptableTypes)
   {
      EnsureInitialized();
      var acceptableInternedTypeIds = _typeIdInterner.GetExistingIds(acceptableTypes);
      if(acceptableInternedTypeIds.Count == 0)
         return [];

      var rows = _connectionPool.UseCommand(
         command => command.SetCommandText($"SELECT {Schema.Id}, {Schema.Value}, {Schema.ValueTypeId} FROM {Schema.TableName} WHERE {Schema.ValueTypeId} {TypeInClause(acceptableInternedTypeIds)}")
                           .ExecuteReaderAndSelect(reader => (SerializedDocument: reader.GetString(1), InternedTypeId: reader.GetInt32(2))));
      return rows.Select(ToReadRow).ToList();
   }

   // Resolve the interned id back to its canonical type string after the reader has closed — never while a connection is held.
   IDocumentDbSqlLayer.ReadRow ToReadRow((string SerializedDocument, int InternedTypeId) row) =>
      new(_typeIdInterner.GetCanonicalString(row.InternedTypeId), row.SerializedDocument);

   static string TypeInClause(IReadOnlySet<int> acceptableInternedTypeIds) => $"IN ({string.Join(", ", acceptableInternedTypeIds)})\n";

   void EnsureInitialized() => _schemaManager.EnsureSchemaInitialized();
}
