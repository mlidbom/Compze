using Compze.Core.DocumentDb.Internal.SqlLayer;
using Compze.Core.DocumentDb.Internal.SqlLayer.Exceptions;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Sql.Common;
using Compze.Utilities.Contracts;
using Compze.Functional;
using Compze.Utilities.SystemCE;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Schema = Compze.Core.DocumentDb.Internal.SqlLayer.IDocumentDbSqlLayer.DocumentTableSchemaStrings;

namespace Compze.Sql.Sqlite.Private.DocumentDb;

public partial class SqliteDocumentDbSqlLayer : IDocumentDbSqlLayer
{
   readonly ISqliteConnectionPool _connectionPool;
   readonly SqliteSqlLayerSchemaManager _schemaManager;

   public SqliteDocumentDbSqlLayer(ISqliteConnectionPool connectionPool, SqliteSqlLayerSchemaManager schemaManager)
   {
      _schemaManager = schemaManager;
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
                                 .AddVarcharParameter(Schema.Id, 500, writeRow.Id)
                                 .AddDateTime2Parameter(Schema.Updated, writeRow.UpdateTime)
                                 .AddVarcharParameter(Schema.ValueTypeId, 36, writeRow.TypeId.ToString())
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
                           .AddVarcharParameter(Schema.Id, 500, idString)
                           .ExecuteReaderAndSelect(reader => new IDocumentDbSqlLayer.ReadRow(Guid.Parse(reader.GetString(1)), reader.GetString(0))));
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
                   .AddVarcharParameter(Schema.Id, 500, row.Id)
                   .AddVarcharParameter(Schema.ValueTypeId, 36, row.TypeId.ToString())
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
                   .AddVarcharParameter(Schema.Id, 500, idString)
                   .ExecuteNonQuery());
   }

   public IEnumerable<Guid> GetAllIds(IReadOnlySet<TypeId> acceptableTypes)
   {
      EnsureInitialized();
      return _connectionPool.UseCommand(
         command => command.SetCommandText($"SELECT {Schema.Id} FROM {Schema.TableName} WHERE {Schema.ValueTypeId} {TypeInClause(acceptableTypes)}")
                           .ExecuteReaderAndSelect(reader => Guid.Parse(reader.GetString(0))));
   }

   public IReadOnlyList<IDocumentDbSqlLayer.ReadRow> GetAll(IEnumerable<Guid> ids, IReadOnlySet<TypeId> acceptableTypes)
   {
      EnsureInitialized();
      return _connectionPool.UseCommand(
         command => command.SetCommandText($"""
                                            SELECT {Schema.Id}, {Schema.Value}, {Schema.ValueTypeId} FROM {Schema.TableName} WHERE {Schema.ValueTypeId} {TypeInClause(acceptableTypes)} 
                                                                               AND {Schema.Id} IN('
                                            """ + ids.Select(id => id.ToString()).Join("','") + "')")
                           .ExecuteReaderAndSelect(reader => new IDocumentDbSqlLayer.ReadRow(Guid.Parse(reader.GetString(2)), reader.GetString(1))));
   }

   public IReadOnlyList<IDocumentDbSqlLayer.ReadRow> GetAll(IReadOnlySet<TypeId> acceptableTypes)
   {
      EnsureInitialized();
      return _connectionPool.UseCommand(
         command => command.SetCommandText($"SELECT {Schema.Id}, {Schema.Value}, {Schema.ValueTypeId} FROM {Schema.TableName} WHERE {Schema.ValueTypeId} {TypeInClause(acceptableTypes)}")
                           .ExecuteReaderAndSelect(reader => new IDocumentDbSqlLayer.ReadRow(Guid.Parse(reader.GetString(2)), reader.GetString(1))));
   }

   static string TypeInClause(IReadOnlySet<TypeId> acceptableTypeIds) => Assert.Argument.Is(acceptableTypeIds.Any())._then("IN( '" + acceptableTypeIds.Select(guid => guid.ToString()).Join("', '") + "')\n");

   void EnsureInitialized() => _schemaManager.EnsureSchemaInitialized();
}
