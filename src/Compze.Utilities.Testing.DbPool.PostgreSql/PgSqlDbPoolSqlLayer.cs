using System;
using Compze.Underscore;
using Compze.Sql.Common;
using Compze.Sql.PostgreSql;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Threading.ResourceAccess;
using Compze.Utilities.Testing.DbPool;
using Npgsql;

#pragma warning disable CA1308 // Normalize strings to uppercase

namespace Compze.Utilities.Testing.DbPool.PostgreSql;

public sealed class PgSqlDbPoolSqlLayer : IDbPoolSqlLayer
{
   public static IComponentRegistrar RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(Singleton.For<IDbPoolSqlLayer>()
                                  .CreatedBy(() => new PgSqlDbPoolSqlLayer())
                                  .DelegateToParentServiceLocatorWhenCloning());

   readonly IPgSqlConnectionPool _masterConnectionPool;

   const string ConnectionStringConfigurationParameterName = "COMPOSABLE_PGSQL_DATABASE_POOL_MASTER_CONNECTIONSTRING";
   readonly IThreadShared<NpgsqlConnectionStringBuilder> _connectionStringBuilder;

   public PgSqlDbPoolSqlLayer()
   {
      var masterConnectionString = Environment.GetEnvironmentVariable(ConnectionStringConfigurationParameterName)
                                ?? "Host=localhost;Database=postgres;Username=postgres;Password=Development!1;";

      _masterConnectionPool = IPgSqlConnectionPool.CreateInstance(masterConnectionString);
      _connectionStringBuilder = IThreadShared.WithDefaultTimeouts(new NpgsqlConnectionStringBuilder(masterConnectionString));
   }

   public string ConnectionStringFor(DbPoolDatabase db)
      => _connectionStringBuilder.Locked(it => it._mutate(me =>
      {
         me.Database = db.Name.ToLowerInvariant();
         me.MinPoolSize = 1;
         me.MaxPoolSize = 10;
         me.ConnectionIdleLifetime = 10;
      }).ConnectionString);

   public void EnsureDatabaseExistsAndIsEmpty(DbPoolDatabase db)
   {
      var databaseName = db.Name.ToLowerInvariant();
      var exists = (string?)_masterConnectionPool.ExecuteScalar($"SELECT datname FROM pg_database WHERE datname = '{databaseName.ToLowerInvariant()}'");
      if(!string.IsNullOrEmpty(exists))
      {
         ResetDatabase(db);
      } else
      {
         _masterConnectionPool.ExecuteNonQuery($"CREATE DATABASE {databaseName};");
      }
   }

   public void ResetDatabase(DbPoolDatabase db)
   {
      ResetConnectionPool(db); // Clear stale connections before DDL
      try
      {
      IPgSqlConnectionPool.CreateInstance(ConnectionStringFor(db)).UseCommand(command => command.SetCommandText("""

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
   }
      catch
      {
         ResetConnectionPool(db); //The connection pool has cached the belief that this database does not exist. We must clear that.
         throw;
      }
   }

   void ResetConnectionPool(DbPoolDatabase db)
   {
      using var connection = new NpgsqlConnection(ConnectionStringFor(db));
      NpgsqlConnection.ClearPool(connection);
   }
}
