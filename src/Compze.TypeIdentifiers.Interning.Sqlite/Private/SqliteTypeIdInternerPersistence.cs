using System.Globalization;
using System.Transactions;
using Compze.Abstractions.Time.Public;
using Compze.Internals.Sql.Common;
using Compze.Internals.Sql.Sqlite;
using Compze.Internals.Sql.Sqlite.Private;
using Compze.Internals.SystemCE.TransactionsCE;
using Types = Compze.TypeIdentifiers.Interning.TypeIdsTableSchema.Types;
using Strings = Compze.TypeIdentifiers.Interning.TypeIdsTableSchema.Strings;
using Names = Compze.TypeIdentifiers.Interning.TypeIdsTableSchema.Names;

namespace Compze.TypeIdentifiers.Interning.Sqlite.Private;

partial class SqliteTypeIdInternerPersistence(ISqliteConnectionPool connectionPool, SqliteSqlLayerSchemaManager schemaManager) : ITypeIdInternerPersistence
{
   readonly ISqliteConnectionPool _connectionPool = connectionPool;
   readonly SqliteSqlLayerSchemaManager _schemaManager = schemaManager;

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
         NullableInt(command.SetCommandText($"SELECT {Strings.TypeId} FROM {Strings.TableName} WHERE {Strings.TypeString} = @{Strings.TypeString} LIMIT 1")
                            .AddMediumTextParameter(Strings.TypeString, spelling)
                            .ExecuteScalar())), TransactionScopeOption.Suppress);

   static int? NullableInt(object? scalar) => scalar is null or DBNull ? null : Convert.ToInt32(scalar, CultureInfo.InvariantCulture);
   static int NonNullInt(object? scalar) => Convert.ToInt32(scalar, CultureInfo.InvariantCulture);

   public T MutateUnderWriteLock<T>(Func<IInternerWriteSession, T> work) =>
      // The interner owns its database, so a mint must never join a business transaction — that would pull two
      // SQLite databases into one System.Transactions transaction. RequiresNew suspends any ambient business
      // transaction and runs the mint in its own: the connection's per-database write gate serialises minters in
      // this process, and the single transaction makes the find-then-insert atomic. The UNIQUE index on the
      // spelling is the cross-process backstop (see the schema), so a mint can never fork a type's identity.
      TransactionScopeCe.Execute(
         () => _connectionPool.UseConnection(connection => work(new WriteSession(connection))),
         TransactionScopeOption.RequiresNew);

   sealed class WriteSession(ICompzeSqliteConnection connection) : IInternerWriteSession
   {
      readonly ICompzeSqliteConnection _connection = connection;

      public int? FindBySpelling(string spelling) => _connection.UseCommand(command =>
         NullableInt(command.SetCommandText($"SELECT {Strings.TypeId} FROM {Strings.TableName} WHERE {Strings.TypeString} = @{Strings.TypeString} LIMIT 1")
                            .AddMediumTextParameter(Strings.TypeString, spelling)
                            .ExecuteScalar()));

      public string? CurrentNameOf(int typeId) => _connection.UseCommand(command =>
         command.SetCommandText($"SELECT {Types.CurrentName} FROM {Types.TableName} WHERE {Types.Id} = @{Types.Id}")
                .AddParameter(Types.Id, typeId)
                .ExecuteScalar() as string);

      public int InsertType(string fullyQualifiedName, string spelling)
      {
         var now = UtcTimeSource.UtcNow;
         _connection.UseCommand(command =>
            command.SetCommandText($"INSERT INTO {Types.TableName} ({Types.CurrentName}) VALUES (@{Types.CurrentName})")
                   .AddMediumTextParameter(Types.CurrentName, fullyQualifiedName)
                   .ExecuteNonQuery());

         var id = _connection.UseCommand(command =>
            NonNullInt(command.SetCommandText("SELECT last_insert_rowid()").ExecuteScalar()));

         _connection.UseCommand(command =>
            command.SetCommandText($"INSERT INTO {Strings.TableName} ({Strings.TypeString}, {Strings.TypeId}, {Strings.FirstSeenUtc}) VALUES (@{Strings.TypeString}, @{Strings.TypeId}, @{Strings.FirstSeenUtc})")
                   .AddMediumTextParameter(Strings.TypeString, spelling)
                   .AddParameter(Strings.TypeId, id)
                   .AddDateTime2Parameter(Strings.FirstSeenUtc, now)
                   .ExecuteNonQuery());

         _connection.UseCommand(command =>
            command.SetCommandText($"INSERT INTO {Names.TableName} ({Names.TypeId}, {Names.Seq}, {Names.FullyQualifiedName}, {Names.FirstSeenUtc}) VALUES (@{Names.TypeId}, 1, @{Names.FullyQualifiedName}, @{Names.FirstSeenUtc})")
                   .AddParameter(Names.TypeId, id)
                   .AddMediumTextParameter(Names.FullyQualifiedName, fullyQualifiedName)
                   .AddDateTime2Parameter(Names.FirstSeenUtc, now)
                   .ExecuteNonQuery());

         return id;
      }

      public void AddSpelling(int typeId, string spelling) => _connection.UseCommand(command =>
         command.SetCommandText($"INSERT INTO {Strings.TableName} ({Strings.TypeString}, {Strings.TypeId}, {Strings.FirstSeenUtc}) VALUES (@{Strings.TypeString}, @{Strings.TypeId}, @{Strings.FirstSeenUtc})")
                .AddMediumTextParameter(Strings.TypeString, spelling)
                .AddParameter(Strings.TypeId, typeId)
                .AddDateTime2Parameter(Strings.FirstSeenUtc, UtcTimeSource.UtcNow)
                .ExecuteNonQuery());

      public void RecordName(int typeId, string fullyQualifiedName)
      {
         var now = UtcTimeSource.UtcNow;
         _connection.UseCommand(command =>
            command.SetCommandText($"INSERT INTO {Names.TableName} ({Names.TypeId}, {Names.Seq}, {Names.FullyQualifiedName}, {Names.FirstSeenUtc}) SELECT @{Names.TypeId}, COALESCE(MAX({Names.Seq}), 0) + 1, @{Names.FullyQualifiedName}, @{Names.FirstSeenUtc} FROM {Names.TableName} WHERE {Names.TypeId} = @{Names.TypeId}")
                   .AddParameter(Names.TypeId, typeId)
                   .AddMediumTextParameter(Names.FullyQualifiedName, fullyQualifiedName)
                   .AddDateTime2Parameter(Names.FirstSeenUtc, now)
                   .ExecuteNonQuery());

         _connection.UseCommand(command =>
            command.SetCommandText($"UPDATE {Types.TableName} SET {Types.CurrentName} = @{Types.CurrentName} WHERE {Types.Id} = @{Types.Id}")
                   .AddMediumTextParameter(Types.CurrentName, fullyQualifiedName)
                   .AddParameter(Types.Id, typeId)
                   .ExecuteNonQuery());
      }
   }
}
