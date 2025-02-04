using System;
using Compze.Functional;
using Compze.Persistence.Common.AdoCE;
using Compze.Persistence.PgSql.SystemExtensions;
using Compze.SystemCE.ThreadingCE.ResourceAccess;
using Npgsql;

#pragma warning disable CA1308 // Normalize strings to uppercase

namespace Compze.Testing.Persistence.PgSql;

sealed class PgSqlDbPool : DbPool
{
   readonly IPgSqlConnectionPool _masterConnectionPool;

   const string ConnectionStringConfigurationParameterName = "COMPOSABLE_PGSQL_DATABASE_POOL_MASTER_CONNECTIONSTRING";
   readonly IThreadShared<NpgsqlConnectionStringBuilder> _connectionStringBuilder;

   public PgSqlDbPool()
   {
      var masterConnectionString = Environment.GetEnvironmentVariable(ConnectionStringConfigurationParameterName)
                                ?? "Host=localhost;Database=postgres;Username=postgres;Password=Development!1;";

      _masterConnectionPool = IPgSqlConnectionPool.CreateInstance(masterConnectionString);
      _connectionStringBuilder = ThreadShared.WithDefaultTimeout(new NpgsqlConnectionStringBuilder(masterConnectionString));
   }

   protected override string ConnectionStringFor(Database db)
      => _connectionStringBuilder.Update(it => it.mutate(me =>
      {
         me.Database = db.Name.ToLowerInvariant();
         me.MinPoolSize = 1;
         me.MaxPoolSize = 10;
         me.ConnectionIdleLifetime = 10;
      }).ConnectionString);

   protected override void EnsureDatabaseExistsAndIsEmpty(Database db)
   {
      var databaseName = db.Name.ToLowerInvariant();
      ResetConnectionPool(db);
      var exists = (string?)_masterConnectionPool.ExecuteScalar($"SELECT datname FROM pg_database WHERE datname = '{databaseName.ToLowerInvariant()}'");
      if (!string.IsNullOrEmpty(exists))
      {
         ResetDatabase(db);
      } else
      {
         _masterConnectionPool.ExecuteNonQuery($"CREATE DATABASE {databaseName};");
      }
   }

   protected override void ResetDatabase(Database db) =>
      IPgSqlConnectionPool.CreateInstance(ConnectionStringFor(db)).UseCommand(
         command => command.SetCommandText("""

                                           DO $$
                                           DECLARE
                                                   dbRecord RECORD;
                                           BEGIN
                                           	FOR dbRecord IN (SELECT nspname
                                           			FROM pg_catalog.pg_namespace
                                           			WHERE nspname NOT IN ('information_schema', 'pg_catalog', 'pg_toast')) 
                                           	LOOP
                                           			EXECUTE format('DROP SCHEMA %I CASCADE;', dbRecord.nspname);
                                           	END LOOP;
                                           
                                           	CREATE SCHEMA public AUTHORIZATION postgres;
                                           	COMMENT ON SCHEMA public IS 'standard public schema';
                                           	GRANT ALL ON SCHEMA public TO PUBLIC;
                                           	GRANT ALL ON SCHEMA public TO postgres;

                                           END; $$;
                                           """)
                           .PrepareStatement()
                           .ExecuteNonQuery());

   void ResetConnectionPool(Database db)
   {
      using var connection = new NpgsqlConnection(ConnectionStringFor(db));
      NpgsqlConnection.ClearPool(connection);
   }
}