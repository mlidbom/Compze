using System;
using Compze.Functional;
using Compze.Persistence.MySql.SystemExtensions;
using Compze.SystemCE.ThreadingCE.ResourceAccess;
using MySql.Data.MySqlClient;

namespace Compze.Testing.Persistence.MySql;

sealed class MySqlDbPool : DbPool
{
   readonly IMySqlConnectionPool _masterConnectionPool;

   const string ConnectionStringConfigurationParameterName = "COMPOSABLE_MYSQL_DATABASE_POOL_MASTER_CONNECTIONSTRING";

   readonly IThreadShared<MySqlConnectionStringBuilder> _connectionStringBuilder;

   public MySqlDbPool()
   {
      var masterConnectionString = Environment.GetEnvironmentVariable(ConnectionStringConfigurationParameterName)
                                ?? "Server=localhost;Database=mysql;Uid=root;Pwd=Development!1;";

      _masterConnectionPool = IMySqlConnectionPool.CreateInstance(masterConnectionString);
      _connectionStringBuilder = ThreadShared.WithDefaultTimeout(new MySqlConnectionStringBuilder(masterConnectionString));
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
