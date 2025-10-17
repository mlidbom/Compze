using Compze.Sql.MicrosoftSql.Infrastructure;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Testing.DbPool.MicrosoftSql.Databases;
using Microsoft.Data.SqlClient;

namespace Compze.Utilities.Testing.DbPool.MicrosoftSql;

static class MicrosoftSqlDbPoolRegistrar
{
   public static IComponentRegistrar MicrosoftSqlDbPoolAndConnectionPoolForConnectionStringName(this IComponentRegistrar registrar, string connectionStringName)
   {
      registrar.MsSqlDbPoolIfNotAlreadyRegistered();

      return registrar.Register(
         Singleton.For<IMsSqlConnectionPool>()
                  .CreatedBy((MsSqlDbPool pool) => IMsSqlConnectionPool.CreateInstance(() => pool.ConnectionStringFor(connectionStringName))));
   }

   public static IComponentRegistrar MsSqlDbPoolIfNotAlreadyRegistered(this IComponentRegistrar registrar) =>
      MsSqlDbPool.RegisterWith(registrar);
}

class MsSqlDbPool : DbPoolBase
{
   internal static IComponentRegistrar RegisterWith(IComponentRegistrar registrar)
   {
      if(registrar.Container().IsRegistered<MsSqlDbPool>())
         return registrar;

      return registrar.Register(Singleton.For<MsSqlDbPool>()
                                         .CreatedBy(() => new MsSqlDbPool())
                                         .DelegateToParentServiceLocatorWhenCloning());
   }

   readonly string _masterConnectionString;
   readonly IMsSqlConnectionPool _masterConnectionPool;

   const string ConnectionStringConfigurationParameterName = "COMPOSABLE_MSSQL_DATABASE_POOL_MASTER_CONNECTIONSTRING";

   public MsSqlDbPool()
   {
      _masterConnectionString = Environment.GetEnvironmentVariable(ConnectionStringConfigurationParameterName)
                             ?? "Data Source=localhost;Initial Catalog=master;Integrated Security=True;TrustServerCertificate=True;";

      _masterConnectionPool = IMsSqlConnectionPool.CreateInstance(_masterConnectionString);
   }

   protected override string ConnectionStringFor(Database db)
      => new SqlConnectionStringBuilder(_masterConnectionString) { InitialCatalog = db.Name }.ConnectionString;

   protected override void EnsureDatabaseExistsAndIsEmpty(Database db)
   {
      var databaseName = db.Name;
      var exists = (string?)_masterConnectionPool.ExecuteScalar($"select name from sysdatabases where name = '{databaseName}'") == databaseName;
      if(exists)
      {
         ResetDatabase(db);
      } else
      {
         ResetConnectionPool(db);
         var createDatabaseCommand = $"""
                                      CREATE DATABASE [{databaseName}]
                                      ALTER DATABASE [{databaseName}] SET RECOVERY SIMPLE;
                                      ALTER DATABASE[{databaseName}] SET READ_COMMITTED_SNAPSHOT ON
                                      """;

         _masterConnectionPool.ExecuteNonQuery(createDatabaseCommand);
      }
   }

   protected override void ResetDatabase(Database db) =>
      IMsSqlConnectionPool.CreateInstance(ConnectionStringFor(db))
                          .UseConnection(action: connection => connection.DropAllObjectsAndSetReadCommittedSnapshotIsolationLevel());

   protected void ResetConnectionPool(Database db)
   {
      using var connection = new SqlConnection(ConnectionStringFor(db));
      SqlConnection.ClearPool(connection);
   }
}
