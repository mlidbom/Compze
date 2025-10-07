using System.Diagnostics.CodeAnalysis;
using Compze.Abstractions.Internal.Persistence.DocumentDb;
using Compze.Persistence.Common;
using Compze.Persistence.PostgreSql.Infrastructure;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;
using Npgsql;
using Schema = Compze.Abstractions.Internal.Persistence.DocumentDb.IDocumentDbPersistenceLayer.DocumentTableSchemaStrings;

namespace Compze.Persistence.DocumentDb.PostgreSql;

partial class PgSqlDocumentDbPersistenceLayer : IDocumentDbPersistenceLayer
{
   readonly IPgSqlConnectionPool _connectionPool;
   readonly SchemaManager _schemaManager;
   bool _initialized;

   internal PgSqlDocumentDbPersistenceLayer(IPgSqlConnectionPool connectionPool)
   {
      _schemaManager = new SchemaManager(connectionPool);
      _connectionPool = connectionPool;
   }

   public void Update(IReadOnlyList<IDocumentDbPersistenceLayer.WriteRow> toUpdate)
   {
      EnsureInitialized();
      _connectionPool.UseConnection(connection =>
      {
         foreach(var writeRow in toUpdate)
         {
            connection.UseCommand(
               command => command.SetCommandText($"UPDATE {Schema.TableName} SET {Schema.Value} = @{Schema.Value}, {Schema.Updated} = @{Schema.Updated} WHERE {Schema.Id} = @{Schema.Id} AND {Schema.ValueTypeId} = @{Schema.ValueTypeId}")
                                 .AddVarcharParameter(Schema.Id, 500, writeRow.Id)
                                 .AddTimestampWithTimeZone(Schema.Updated, writeRow.UpdateTime)
                                 .AddParameter(Schema.ValueTypeId, writeRow.TypeId)
                                 .AddMediumTextParameter(Schema.Value, writeRow.SerializedDocument)
                                 .PrepareStatement()
                                 .ExecuteNonQuery());
         }
      });
   }

   public bool TryGet(string idString, IReadOnlySet<Guid> acceptableTypeIds, bool useUpdateLock, [NotNullWhen(true)] out IDocumentDbPersistenceLayer.ReadRow? document)
   {
      EnsureInitialized();

      var documents = _connectionPool.UseCommand(
         command => command.SetCommandText($"""

                                            SELECT {Schema.Value}, {Schema.ValueTypeId} FROM {Schema.TableName}  {UseUpdateLock(useUpdateLock)} 
                                            WHERE {Schema.Id}=@{Schema.Id} AND {Schema.ValueTypeId} {TypeInClause(acceptableTypeIds)}
                                            """)
                           .AddVarcharParameter(Schema.Id, 500, idString)
                           .PrepareStatement()
                           .ExecuteReaderAndSelect(reader => new IDocumentDbPersistenceLayer.ReadRow(reader.GetGuid(1), reader.GetString(0))));
      if(documents.Count < 1)
      {
         document = null;
         return false;
      }

      document = documents[0];

      return true;
   }

   public void Add(IDocumentDbPersistenceLayer.WriteRow row)
   {
      EnsureInitialized();
      try
      {
         _connectionPool.UseCommand(command =>
         {

            command.SetCommandText($"INSERT INTO {Schema.TableName}({Schema.Id}, {Schema.ValueTypeId}, {Schema.Value}, {Schema.Created}, {Schema.Updated}) VALUES(@{Schema.Id}, @{Schema.ValueTypeId}, @{Schema.Value}, @{Schema.Created}, @{Schema.Updated})")
                   .AddVarcharParameter(Schema.Id, 500, row.Id)
                   .AddParameter(Schema.ValueTypeId, row.TypeId)
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

   public int Remove(string idString, IReadOnlySet<Guid> acceptableTypes)
   {
      EnsureInitialized();
      return _connectionPool.UseCommand(
         command =>
            command.SetCommandText($"DELETE FROM {Schema.TableName} WHERE {Schema.Id} = @{Schema.Id} AND {Schema.ValueTypeId} {TypeInClause(acceptableTypes)}")
                   .AddVarcharParameter(Schema.Id, 500, idString)
                   .PrepareStatement()
                   .ExecuteNonQuery());
   }

   public IEnumerable<Guid> GetAllIds(IReadOnlySet<Guid> acceptableTypes)
   {
      EnsureInitialized();
      return _connectionPool.UseCommand(
         command => command.SetCommandText($"SELECT {Schema.Id} FROM {Schema.TableName} WHERE {Schema.ValueTypeId} {TypeInClause(acceptableTypes)}")
                           .PrepareStatement()//Performance: Does this work in Npgsql when there are no parameters? Should we have parameters?
                           .ExecuteReaderAndSelect(reader => Guid.Parse(reader.GetString(0))));
   }

   public IReadOnlyList<IDocumentDbPersistenceLayer.ReadRow> GetAll(IEnumerable<Guid> ids, IReadOnlySet<Guid> acceptableTypes)
   {
      EnsureInitialized();
      return _connectionPool.UseCommand(
         command => command.SetCommandText($"""
                                            SELECT {Schema.Id}, {Schema.Value}, {Schema.ValueTypeId} FROM {Schema.TableName} WHERE {Schema.ValueTypeId} {TypeInClause(acceptableTypes)} 
                                                                               AND {Schema.Id} IN('
                                            """ + ids.Select(id => id.ToString()).Join("','") + "')")
                           .PrepareStatement() //Performance: Does this work in Npgsql when there are no parameters? Should we have parameters?
                           .ExecuteReaderAndSelect(reader => new IDocumentDbPersistenceLayer.ReadRow(reader.GetGuid(2), reader.GetString(1))));
   }

   public IReadOnlyList<IDocumentDbPersistenceLayer.ReadRow> GetAll(IReadOnlySet<Guid> acceptableTypes)
   {
      EnsureInitialized();
      return _connectionPool.UseCommand(
         command => command.SetCommandText($"SELECT {Schema.Id}, {Schema.Value}, {Schema.ValueTypeId} FROM {Schema.TableName} WHERE {Schema.ValueTypeId} {TypeInClause(acceptableTypes)}")
                           .PrepareStatement() //Performance: Does this work in Npgsql when there are no parameters? Should we have parameters?
                           .ExecuteReaderAndSelect(reader => new IDocumentDbPersistenceLayer.ReadRow(reader.GetGuid(2), reader.GetString(1))));
   }

   static string TypeInClause(IEnumerable<Guid> acceptableTypeIds) => "IN( '" + acceptableTypeIds.Select(guid => guid.ToString()).Join("', '") + "')\n";

   // ReSharper disable once UnusedParameter.Local
   static string UseUpdateLock(bool _) => "";// useUpdateLock ? "With(UPDLOCK, ROWLOCK)" : "";

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