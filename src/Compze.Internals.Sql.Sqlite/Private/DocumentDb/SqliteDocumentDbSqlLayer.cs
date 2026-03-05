using Compze.Core.DocumentDb.Internal.SqlLayer;
using Compze.Core.DocumentDb.Internal.SqlLayer.Exceptions;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Internals.Sql.Common;
using Compze.Contracts;
using Compze.Underscore;
using Compze.Internals.SystemCE;
using Microsoft.Data.Sqlite;
using System.Diagnostics.CodeAnalysis;
using Schema = Compze.Core.DocumentDb.Internal.SqlLayer.IDocumentDbSqlLayer.DocumentTableSchemaStrings;

namespace Compze.Internals.Sql.Sqlite.Private.DocumentDb;

partial class SqliteDocumentDbSqlLayer(ISqliteConnectionPool connectionPool, SqliteSqlLayerSchemaManager schemaManager) : IDocumentDbSqlLayer
{
   readonly ISqliteConnectionPool _connectionPool = connectionPool;
   readonly SqliteSqlLayerSchemaManager _schemaManager = schemaManager;

   public void Update(IReadOnlyList<IDocumentDbSqlLayer.WriteRow> toUpdate)
   {
      EnsureInitialized();
      _connectionPool.UseConnection(connection =>
      {
         foreach(var writeRow in toUpdate)
         {
            connection.UseCommand(
               command => command.SetCommandText($"UPDATE {Schema.TableName} SET {Schema.Value} = @{Schema.Value}, {Schema.Updated} = @{Schema.Updated} WHERE {Schema.Id} = @{Schema.Id} AND {Schema.ValueTypeId} = @{Schema.ValueTypeId}")
                                 .AddMediumTextParameter(Schema.Id, writeRow.Id)
                                 .AddDateTime2Parameter(Schema.Updated, writeRow.UpdateTime)
                                 .AddMediumTextParameter(Schema.ValueTypeId, writeRow.TypeId.ToString())
                                 .AddMediumTextParameter(Schema.Value, writeRow.SerializedDocument)
                                 .ExecuteNonQuery());
         }
      });
   }

   public bool TryGet(string idString, IReadOnlySet<TypeId> acceptableTypeIds, bool useUpdateLock, [NotNullWhen(true)] out IDocumentDbSqlLayer.ReadRow? document)
   {
      EnsureInitialized();

      var documents = _connectionPool.UseCommand(
         command => command.SetCommandText($"""

                                            SELECT {Schema.Value}, {Schema.ValueTypeId} FROM {Schema.TableName} 
                                            WHERE {Schema.Id}=@{Schema.Id} AND {Schema.ValueTypeId} {TypeInClause(acceptableTypeIds)}
                                            """)
                           .AddMediumTextParameter(Schema.Id, idString)
                           .ExecuteReaderAndSelect(reader => new IDocumentDbSqlLayer.ReadRow(reader.GetGuidFromString(1), reader.GetString(0))));
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
                   .AddMediumTextParameter(Schema.Id, row.Id)
                   .AddMediumTextParameter(Schema.ValueTypeId, row.TypeId.ToString())
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

   public int Remove(string idString, IReadOnlySet<TypeId> acceptableTypes)
   {
      EnsureInitialized();
      return _connectionPool.UseCommand(
         command =>
            command.SetCommandText($"DELETE FROM {Schema.TableName} WHERE {Schema.Id} = @{Schema.Id} AND {Schema.ValueTypeId} {TypeInClause(acceptableTypes)}")
                   .AddMediumTextParameter(Schema.Id, idString)
                   .ExecuteNonQuery());
   }

   public IEnumerable<Guid> GetAllIds(IReadOnlySet<TypeId> acceptableTypes)
   {
      EnsureInitialized();
      return _connectionPool.UseCommand(
         command => command.SetCommandText($"SELECT {Schema.Id} FROM {Schema.TableName} WHERE {Schema.ValueTypeId} {TypeInClause(acceptableTypes)}")
                           .ExecuteReaderAndSelect(reader => reader.GetGuidFromString(0)));
   }

   public IReadOnlyList<IDocumentDbSqlLayer.ReadRow> GetAll(IEnumerable<Guid> ids, IReadOnlySet<TypeId> acceptableTypes)
   {
      EnsureInitialized();
      return _connectionPool.UseCommand(
         command => command.SetCommandText($"""
                                            SELECT {Schema.Id}, {Schema.Value}, {Schema.ValueTypeId} FROM {Schema.TableName} WHERE {Schema.ValueTypeId} {TypeInClause(acceptableTypes)} 
                                                                               AND {Schema.Id} IN('
                                            """ + ids.Select(id => id.ToString()).Join("','") + "')")
                           .ExecuteReaderAndSelect(reader => new IDocumentDbSqlLayer.ReadRow(reader.GetGuidFromString(2), reader.GetString(1))));
   }

   public IReadOnlyList<IDocumentDbSqlLayer.ReadRow> GetAll(IReadOnlySet<TypeId> acceptableTypes)
   {
      EnsureInitialized();
      return _connectionPool.UseCommand(
         command => command.SetCommandText($"SELECT {Schema.Id}, {Schema.Value}, {Schema.ValueTypeId} FROM {Schema.TableName} WHERE {Schema.ValueTypeId} {TypeInClause(acceptableTypes)}")
                           .ExecuteReaderAndSelect(reader => new IDocumentDbSqlLayer.ReadRow(Guid.Parse(reader.GetString(2)), reader.GetString(1))));
   }

   static string TypeInClause(IReadOnlySet<TypeId> acceptableTypeIds) => Contract.Argument.Assert(acceptableTypeIds.Any())._then("IN( '" + acceptableTypeIds.Select(guid => guid.ToString()).Join("', '") + "')\n");

   void EnsureInitialized() => _schemaManager.EnsureSchemaInitialized();
}
