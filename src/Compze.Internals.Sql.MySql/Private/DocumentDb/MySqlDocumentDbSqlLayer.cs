using Compze.DocumentDb.Internal.SqlLayer;
using Compze.DocumentDb.Internal.SqlLayer.Exceptions;
using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Internals.Sql.Common;
using Compze.Internals.SystemCE;
using MySql.Data.MySqlClient;
using System.Diagnostics.CodeAnalysis;
using Schema = Compze.DocumentDb.Internal.SqlLayer.IDocumentDbSqlLayer.DocumentTableSchemaStrings;

namespace Compze.Internals.Sql.MySql.Private.DocumentDb;

partial class MySqlDocumentDbSqlLayer(IMySqlConnectionPool connectionPool, MySqlSqlLayerSchemaManager schemaManager) : IDocumentDbSqlLayer
{
   readonly IMySqlConnectionPool _connectionPool = connectionPool;
   readonly MySqlSqlLayerSchemaManager _schemaManager = schemaManager;

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
                                 .AddParameter(Schema.ValueTypeId, writeRow.TypeId.GuidValue)
                                 .AddMediumTextParameter(Schema.Value, writeRow.SerializedDocument)
                                 .ExecuteNonQuery());
         }
      });
   }

   public bool TryGet(string idString, IReadOnlySet<MappedTypeId> acceptableTypeIds, bool useUpdateLock, [NotNullWhen(true)] out IDocumentDbSqlLayer.ReadRow? document)
   {
      EnsureInitialized();

      var documents = _connectionPool.UseCommand(
         command => command.SetCommandText($"""

                                            SELECT {Schema.Value}, {Schema.ValueTypeId} FROM {Schema.TableName} {UseUpdateLock(useUpdateLock)} 
                                            WHERE {Schema.Id}=@{Schema.Id} AND {Schema.ValueTypeId} {TypeInClause(acceptableTypeIds)}
                                            """)
                           .AddVarcharParameter(Schema.Id, 500, idString)
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
                   .AddVarcharParameter(Schema.Id, 500, row.Id)
                   .AddParameter(Schema.ValueTypeId, row.TypeId.GuidValue)
                   .AddDateTime2Parameter(Schema.Created, row.UpdateTime)
                   .AddDateTime2Parameter(Schema.Updated, row.UpdateTime)
                   .AddMediumTextParameter(Schema.Value, row.SerializedDocument)
                   .ExecuteNonQuery();
         });
      }
      catch(MySqlException exception)when(SqlExceptions.MySql.IsPrimaryKeyViolation(exception))
      {
         throw new AttemptToSaveAlreadyPersistedValueException(row.Id, row.SerializedDocument);
      }
   }

   public int Remove(string idString, IReadOnlySet<MappedTypeId> acceptableTypes)
   {
      EnsureInitialized();
      return _connectionPool.UseCommand(
         command =>
            command.SetCommandText($"DELETE FROM {Schema.TableName} WHERE {Schema.Id} = @{Schema.Id} AND {Schema.ValueTypeId} {TypeInClause(acceptableTypes)}")
                   .AddVarcharParameter(Schema.Id, 500, idString)
                   .ExecuteNonQuery());
   }

   public IEnumerable<Guid> GetAllIds(IReadOnlySet<MappedTypeId> acceptableTypes)
   {
      EnsureInitialized();
      return _connectionPool.UseCommand(
         command => command.SetCommandText($"SELECT {Schema.Id} FROM {Schema.TableName} WHERE {Schema.ValueTypeId} {TypeInClause(acceptableTypes)}")
                           .ExecuteReaderAndSelect(reader => reader.GetGuidFromString(0)));
   }

   public IReadOnlyList<IDocumentDbSqlLayer.ReadRow> GetAll(IEnumerable<Guid> ids, IReadOnlySet<MappedTypeId> acceptableTypes)
   {
      EnsureInitialized();
      return _connectionPool.UseCommand(
         command => command.SetCommandText($"""
                                            SELECT {Schema.Id}, {Schema.Value}, {Schema.ValueTypeId} FROM {Schema.TableName} WHERE {Schema.ValueTypeId} {TypeInClause(acceptableTypes)} 
                                                                               AND {Schema.Id} IN('
                                            """ + ids.Select(id => id.ToString()).Join("','") + "')")
                           .ExecuteReaderAndSelect(reader => new IDocumentDbSqlLayer.ReadRow(reader.GetGuid(2), reader.GetString(1))));
   }

   public IReadOnlyList<IDocumentDbSqlLayer.ReadRow> GetAll(IReadOnlySet<MappedTypeId> acceptableTypes)
   {
      EnsureInitialized();
      return _connectionPool.UseCommand(
         command => command.SetCommandText($"SELECT {Schema.Id}, {Schema.Value}, {Schema.ValueTypeId} FROM {Schema.TableName} WHERE {Schema.ValueTypeId} {TypeInClause(acceptableTypes)}")
                           .ExecuteReaderAndSelect(reader => new IDocumentDbSqlLayer.ReadRow(reader.GetGuid(2), reader.GetString(1))));
   }

   static string TypeInClause(IEnumerable<MappedTypeId> acceptableTypeIds) => "IN( '" + acceptableTypeIds.Select(id => id.GuidValue.ToString()).Join("', '") + "')\n";

   // ReSharper disable once UnusedParameter.Local
   static string UseUpdateLock(bool _) => "";// useUpdateLock ? "With(UPDLOCK, ROWLOCK)" : "";

   void EnsureInitialized() => _schemaManager.EnsureSchemaInitialized();
}