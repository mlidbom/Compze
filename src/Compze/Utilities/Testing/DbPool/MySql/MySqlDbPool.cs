using System;
using Compze.Sql.MySql.SystemExtensions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Functional;
using Compze.Utilities.Threading.ResourceAccess;
using MySql.Data.MySqlClient;

namespace Compze.Utilities.Testing.DbPool.MySql;

static class MySqlDbPoolRegistrar
{
   public static IComponentRegistrar MySqlDbPoolIfNotAlreadyRegistered(this IComponentRegistrar registrar) =>
      MySqlDbPool.RegisterWith(registrar);

   public static IComponentRegistrar MySqlDbPoolWithConnectionPoolForConnectionStringName(this IComponentRegistrar registrar, string connectionStringName)
   {
      registrar.MySqlDbPoolIfNotAlreadyRegistered();

      return registrar.Register(
         Singleton.For<IMySqlConnectionPool>()
                  .CreatedBy((MySqlDbPool pool) => IMySqlConnectionPool.CreateInstance(() => pool.ConnectionStringFor(connectionStringName)))
      );
   }
}

sealed class MySqlDbPool : DbPoolBase
{
   internal static IComponentRegistrar RegisterWith(IComponentRegistrar registrar)
   {
      if(registrar.Container().IsRegistered<MySqlDbPool>())
         return registrar;

      return registrar.Register(Singleton.For<MySqlDbPool>()
                                         .CreatedBy(() => new MySqlDbPool())
                                         .DelegateToParentServiceLocatorWhenCloning());
   }

   readonly IMySqlConnectionPool _masterConnectionPool;

   const string ConnectionStringConfigurationParameterName = "COMPOSABLE_MYSQL_DATABASE_POOL_MASTER_CONNECTIONSTRING";

   readonly IThreadShared<MySqlConnectionStringBuilder> _connectionStringBuilder;

   public MySqlDbPool()
   {
      var masterConnectionString = Environment.GetEnvironmentVariable(ConnectionStringConfigurationParameterName)
                                ?? "Server=localhost;Database=mysql;Uid=root;Pwd=Development!1;";

      _masterConnectionPool = IMySqlConnectionPool.CreateInstance(masterConnectionString);
      _connectionStringBuilder = IThreadShared.WithDefaultTimeout(new MySqlConnectionStringBuilder(masterConnectionString));
   }

   protected override string ConnectionStringFor(Database db)
      => _connectionStringBuilder.Update(it => it.mutate(me =>
      {
         me.Database = db.Name;
         me.MinimumPoolSize = 1;
         me.MaximumPoolSize = 10;
         me.ConnectionLifeTime = 10;
      }).ConnectionString);

   protected override void EnsureDatabaseExistsAndIsEmpty(Database db)
   {
      ResetConnectionPool(db);
      ResetDatabase(db);
   }

   protected override void ResetDatabase(Database db)
   {
      //I experimented with dropping objects like for the other databases, but it was not faster than just dropping and recreating the database.
      _masterConnectionPool.ExecuteNonQuery($"""
                                             DROP DATABASE IF EXISTS {db.Name};
                                             CREATE DATABASE {db.Name};
                                             """);
   }

   void ResetConnectionPool(Database db)
   {
      using var connection = new MySqlConnection(ConnectionStringFor(db));
      MySqlConnection.ClearPool(connection);
   }
}
