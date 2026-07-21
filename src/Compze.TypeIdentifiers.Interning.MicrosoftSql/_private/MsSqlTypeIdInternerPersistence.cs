using System.Globalization;
using System.Transactions;
using Compze.Abstractions.Time;
using Compze.Sql.Common._internal;
using Compze.Sql.MicrosoftSql._internal;
using Compze.Internals.SystemCE.TransactionsCE;
using Types = Compze.TypeIdentifiers.Interning.TypeIdsTableSchema.Types;
using Strings = Compze.TypeIdentifiers.Interning.TypeIdsTableSchema.Strings;
using Names = Compze.TypeIdentifiers.Interning.TypeIdsTableSchema.Names;

namespace Compze.TypeIdentifiers.Interning.MicrosoftSql._private;

partial class MsSqlTypeIdInternerPersistence(IMsSqlConnectionPool connectionPool, MsSqlSqlLayerSchemaManager schemaManager) : ITypeIdInternerPersistence
{
   readonly IMsSqlConnectionPool _connectionPool = connectionPool;
   readonly MsSqlSqlLayerSchemaManager _schemaManager = schemaManager;

   // The application-lock resource serialising interner writes across every process pointed at this database.
   const string LockResource = "Compze.TypeIdInterner.Write";

   public void EnsureInitialized() => _schemaManager.EnsureSchemaInitialized();

   public InternerSnapshot LoadAll() => TransactionScopeCe.Execute(() =>
   {
      var types = _connectionPool.UseCommand(command =>
         command.SetCommandText($"SELECT {Types.Id}, {Types.CurrentName} FROM {Types.TableName}")
                .ExecuteReaderAndSelect(reader => (reader.GetInt32(0), reader.GetString(1))));

      var spellings = _connectionPool.UseCommand(command =>
         command.SetCommandText($"SELECT {Strings.TypeId}, {Strings.TypeString} FROM {Strings.TableName}")
                .ExecuteReaderAndSelect(reader => (reader.GetInt32(0), reader.GetString(1))));

      return new InternerSnapshot(types, spellings);
   }, TransactionScopeOption.Suppress);

   public int? FindIdBySpelling(string spelling) => TransactionScopeCe.Execute(() =>
      _connectionPool.UseCommand(command =>
         NullableInt(command.SetCommandText($"SELECT TOP 1 {Strings.TypeId} FROM {Strings.TableName} WHERE {Strings.TypeString} = @{Strings.TypeString}")
                            .AddNVarcharMaxParameter(Strings.TypeString, spelling)
                            .ExecuteScalar())), TransactionScopeOption.Suppress);

   static int? NullableInt(object? scalar) => scalar is null or DBNull ? null : Convert.ToInt32(scalar, CultureInfo.InvariantCulture);
   static int NonNullInt(object? scalar) => Convert.ToInt32(scalar, CultureInfo.InvariantCulture);

   public T MutateUnderWriteLock<T>(Func<IInternerWriteSession, T> work) => TransactionScopeCe.Execute(() =>
      _connectionPool.UseConnection(connection =>
      {
         connection.UseCommand(command =>
            command.SetCommandText("EXEC sp_getapplock @Resource = @resource, @LockMode = 'Exclusive', @LockOwner = 'Session'")
                   .AddNVarcharParameter("resource", 255, LockResource)
                   .ExecuteNonQuery());
         try
         {
            return work(new WriteSession(connection));
         }
         finally
         {
            connection.UseCommand(command =>
               command.SetCommandText("EXEC sp_releaseapplock @Resource = @resource, @LockOwner = 'Session'")
                      .AddNVarcharParameter("resource", 255, LockResource)
                      .ExecuteNonQuery());
         }
      }), TransactionScopeOption.Suppress);

   sealed class WriteSession(ICompzeMsSqlConnection connection) : IInternerWriteSession
   {
      readonly ICompzeMsSqlConnection _connection = connection;

      public int? FindBySpelling(string spelling) => _connection.UseCommand(command =>
         NullableInt(command.SetCommandText($"SELECT TOP 1 {Strings.TypeId} FROM {Strings.TableName} WHERE {Strings.TypeString} = @{Strings.TypeString}")
                            .AddNVarcharMaxParameter(Strings.TypeString, spelling)
                            .ExecuteScalar()));

      public string? CurrentNameOf(int typeId) => _connection.UseCommand(command =>
         command.SetCommandText($"SELECT {Types.CurrentName} FROM {Types.TableName} WHERE {Types.Id} = @{Types.Id}")
                .AddParameter(Types.Id, typeId)
                .ExecuteScalar() as string);

      public int InsertType(string fullyQualifiedName, string spelling) => _connection.UseCommand(command =>
         NonNullInt(command.SetCommandText($"""
                                            INSERT INTO {Types.TableName} ({Types.CurrentName}) VALUES (@{Types.CurrentName});
                                            DECLARE @newId int = CAST(SCOPE_IDENTITY() AS int);
                                            INSERT INTO {Strings.TableName} ({Strings.TypeString}, {Strings.TypeId}, {Strings.FirstSeenUtc}) VALUES (@{Strings.TypeString}, @newId, @{Strings.FirstSeenUtc});
                                            INSERT INTO {Names.TableName} ({Names.TypeId}, {Names.Seq}, {Names.FullyQualifiedName}, {Names.FirstSeenUtc}) VALUES (@newId, 1, @{Types.CurrentName}, @{Strings.FirstSeenUtc});
                                            SELECT @newId;
                                            """)
                           .AddNVarcharMaxParameter(Types.CurrentName, fullyQualifiedName)
                           .AddNVarcharMaxParameter(Strings.TypeString, spelling)
                           .AddDateTime2Parameter(Strings.FirstSeenUtc, UtcTimeSource.UtcNow)
                           .ExecuteScalar()));

      public void AddSpelling(int typeId, string spelling) => _connection.UseCommand(command =>
         command.SetCommandText($"INSERT INTO {Strings.TableName} ({Strings.TypeString}, {Strings.TypeId}, {Strings.FirstSeenUtc}) VALUES (@{Strings.TypeString}, @{Strings.TypeId}, @{Strings.FirstSeenUtc})")
                .AddNVarcharMaxParameter(Strings.TypeString, spelling)
                .AddParameter(Strings.TypeId, typeId)
                .AddDateTime2Parameter(Strings.FirstSeenUtc, UtcTimeSource.UtcNow)
                .ExecuteNonQuery());

      public void RecordName(int typeId, string fullyQualifiedName) => _connection.UseCommand(command =>
         command.SetCommandText($"""
                                 DECLARE @nextSeq int = (SELECT COALESCE(MAX({Names.Seq}), 0) + 1 FROM {Names.TableName} WHERE {Names.TypeId} = @{Names.TypeId});
                                 INSERT INTO {Names.TableName} ({Names.TypeId}, {Names.Seq}, {Names.FullyQualifiedName}, {Names.FirstSeenUtc}) VALUES (@{Names.TypeId}, @nextSeq, @{Names.FullyQualifiedName}, @{Names.FirstSeenUtc});
                                 UPDATE {Types.TableName} SET {Types.CurrentName} = @{Names.FullyQualifiedName} WHERE {Types.Id} = @{Names.TypeId};
                                 """)
                .AddParameter(Names.TypeId, typeId)
                .AddNVarcharMaxParameter(Names.FullyQualifiedName, fullyQualifiedName)
                .AddDateTime2Parameter(Names.FirstSeenUtc, UtcTimeSource.UtcNow)
                .ExecuteNonQuery());
   }
}
