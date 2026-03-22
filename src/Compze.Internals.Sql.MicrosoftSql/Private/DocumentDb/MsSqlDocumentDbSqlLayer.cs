using Compze.DocumentDb.Internal.SqlLayer;
using Compze.DocumentDb.Internal.SqlLayer.Exceptions;
using Compze.TypeIdentifiers;
using Compze.Internals.Sql.Common;
using Compze.Contracts;
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
                  .CreatedBy((IMsSqlConnectionPool connectionProvider, MsSqlSqlLayerSchemaManager schemaManager) => new MsSqlDocumentDbSqlLayer(connectionProvider, schemaManager)));

   readonly IMsSqlConnectionPool _connectionPool;
   readonly MsSqlSqlLayerSchemaManager _schemaManager;

   MsSqlDocumentDbSqlLayer(IMsSqlConnectionPool connectionPool, MsSqlSqlLayerSchemaManager schemaManager)
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
            connection.UseCommand(command => command.SetCommandText($"UPDATE {Schema.TableName} SET {Schema.Value} = @{Schema.Value}, {Schema.Updated} = @{Schema.Updated} WHERE {Schema.Id} = @{Schema.Id} AND {Schema.ValueTypeId} = @{Schema.ValueTypeId}")
                                                    .AddNVarcharParameter(Schema.Id, 500, writeRow.Id)
                                                    .AddDateTime2Parameter(Schema.Updated, writeRow.UpdateTime)
                                                    .AddParameter(Schema.ValueTypeId, writeRow.TypeId.GuidValue)
                                                    .AddNVarcharMaxParameter(Schema.Value, writeRow.SerializedDocument)
                                                    .ExecuteNonQuery());
         }
      });
   }

   public bool TryGet(string idString, IReadOnlySet<MappedTypeIdentifier> acceptableTypeIds, bool useUpdateLock, [NotNullWhen(true)] out IDocumentDbSqlLayer.ReadRow? document)
   {
      EnsureInitialized();

      var documents = _connectionPool.UseCommand(command => command.SetCommandText($"""

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
                   .AddParameter(Schema.ValueTypeId, row.TypeId.GuidValue)
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

   public int Remove(string idString, IReadOnlySet<MappedTypeIdentifier> acceptableTypes)
   {
      EnsureInitialized();
      return _connectionPool.UseCommand(command =>
                                           command.SetCommandText($"DELETE FROM {Schema.TableName} WHERE {Schema.Id} = @{Schema.Id} AND {Schema.ValueTypeId} {TypeInClause(acceptableTypes)}")
                                                  .AddNVarcharParameter(Schema.Id, 500, idString)
                                                  .ExecuteNonQuery());
   }

   public IEnumerable<Guid> GetAllIds(IReadOnlySet<MappedTypeIdentifier> acceptableTypes)
   {
      EnsureInitialized();
      return _connectionPool.UseCommand(command => command.SetCommandText($"SELECT {Schema.Id} FROM {Schema.TableName} WHERE {Schema.ValueTypeId} {TypeInClause(acceptableTypes)}")
                                                          .ExecuteReaderAndSelect(reader => reader.GetGuidFromString(0))); //urgent: Huh, we store string but require them to be Guid!?
   }

   public IReadOnlyList<IDocumentDbSqlLayer.ReadRow> GetAll(IEnumerable<Guid> ids, IReadOnlySet<MappedTypeIdentifier> acceptableTypes)
   {
      EnsureInitialized();
      return _connectionPool.UseCommand(command => command.SetCommandText($"""
                                                                           SELECT {Schema.Id}, {Schema.Value}, {Schema.ValueTypeId} FROM {Schema.TableName} WHERE {Schema.ValueTypeId} {TypeInClause(acceptableTypes)} 
                                                                                                              AND {Schema.Id} IN('
                                                                           """ + ids.Select(id => id.ToString()).Join("','") + "')")
                                                          .ExecuteReaderAndSelect(reader => new IDocumentDbSqlLayer.ReadRow(reader.GetGuid(2), reader.GetString(1))));
   }

   public IReadOnlyList<IDocumentDbSqlLayer.ReadRow> GetAll(IReadOnlySet<MappedTypeIdentifier> acceptableTypes)
   {
      EnsureInitialized();
      return _connectionPool.UseCommand(command => command.SetCommandText($"SELECT {Schema.Id}, {Schema.Value}, {Schema.ValueTypeId} FROM {Schema.TableName} WHERE {Schema.ValueTypeId} {TypeInClause(acceptableTypes)}")
                                                          .ExecuteReaderAndSelect(reader => new IDocumentDbSqlLayer.ReadRow(reader.GetGuid(2), reader.GetString(1))));
   }

   static string TypeInClause(IReadOnlySet<MappedTypeIdentifier> acceptableTypeIds) => Contract.Argument.Assert(acceptableTypeIds.Any()).__("IN( '" + acceptableTypeIds.Select(id => id.GuidValue.ToString()).Join("', '") + "')\n");

   static string UseUpdateLock(bool useUpdateLock) => useUpdateLock ? "With(UPDLOCK, ROWLOCK)" : "";

   void EnsureInitialized() => _schemaManager.EnsureSchemaInitialized();
}
