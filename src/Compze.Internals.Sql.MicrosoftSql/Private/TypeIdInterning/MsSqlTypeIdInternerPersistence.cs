using System.Globalization;
using Compze.Internals.Sql.Common;
using Compze.Internals.Sql.Common.Abstractions;
using Schema = Compze.Internals.Sql.Common.Abstractions.TypeIdsTableSchema;

namespace Compze.Internals.Sql.MicrosoftSql.Private.TypeIdInterning;

partial class MsSqlTypeIdInternerPersistence(IMsSqlConnectionPool connectionPool, MsSqlSqlLayerSchemaManager schemaManager) : ITypeIdInternerPersistence
{
   readonly IMsSqlConnectionPool _connectionPool = connectionPool;
   readonly MsSqlSqlLayerSchemaManager _schemaManager = schemaManager;

   // SQL Server is MVCC: a suppressed insert commits independently of the business transaction without a
   // writer-lock conflict, so interning runs in a suppressed scope.
   public bool SuppressAmbientTransactionBeforeAllCalls => true;

   public void EnsureInitialized() => _schemaManager.EnsureSchemaInitialized();

   public IEnumerable<(int Id, string TypeString)> LoadAll() => _connectionPool.UseCommand(command =>
      command.SetCommandText($"SELECT {Schema.Id}, {Schema.TypeString} FROM {Schema.TableName}")
             .ExecuteReaderAndSelect(reader => (reader.GetInt32(0), reader.GetString(1))));

   public int InsertOrGet(string typeString)
   {
      // The UPDLOCK + HOLDLOCK range lock on the existence check serializes concurrent inserts of the same
      // string, so the check-and-insert is atomic; the UNIQUE constraint is the backstop.
      _connectionPool.UseCommand(command =>
         command.SetCommandText($"""
                                 INSERT INTO {Schema.TableName} ({Schema.TypeString})
                                 SELECT @{Schema.TypeString}
                                 WHERE NOT EXISTS (SELECT 1 FROM {Schema.TableName} WITH (UPDLOCK, HOLDLOCK) WHERE {Schema.TypeString} = @{Schema.TypeString})
                                 """)
                .AddNVarcharParameter(Schema.TypeString, TypeStringMaxLength, typeString)
                .ExecuteNonQuery());
      return TryGetId(typeString) ?? throw new InvalidOperationException($"Failed to insert or retrieve an interned id for type string '{typeString}'.");
   }

   public int? TryGetId(string typeString) => _connectionPool.UseCommand(command =>
      NullableInt(command.SetCommandText($"SELECT {Schema.Id} FROM {Schema.TableName} WHERE {Schema.TypeString} = @{Schema.TypeString}")
                         .AddNVarcharParameter(Schema.TypeString, TypeStringMaxLength, typeString)
                         .ExecuteScalar()));

   public string? GetById(int id) => _connectionPool.UseCommand(command =>
      command.SetCommandText($"SELECT {Schema.TypeString} FROM {Schema.TableName} WHERE {Schema.Id} = @{Schema.Id}")
             .AddParameter(Schema.Id, id)
             .ExecuteScalar() as string);

   static int? NullableInt(object? scalar) => scalar is null or DBNull ? null : Convert.ToInt32(scalar, CultureInfo.InvariantCulture);
}
