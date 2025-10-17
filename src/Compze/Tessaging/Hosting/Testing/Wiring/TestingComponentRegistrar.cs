using System;
using System.Collections.Generic;
using Compze.Tessaging.Hosting.Sql.MicrosoftSql;
using Compze.Tessaging.Hosting.Sql.MySql;
using Compze.Tessaging.Hosting.Sql.PostgreSql;
using Compze.Tessaging.Hosting.Sql.Sqlite;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Testing.DbPool.MicrosoftSql;

namespace Compze.Tessaging.Hosting.Testing.Wiring;

class TestingComponentRegistrar : ComponentRegistrar
{
   readonly IDictionary<Type, object> _testingRegistrars;

   public TestingComponentRegistrar()
   {
      _testingRegistrars = new Dictionary<Type, object>()
                           {
                              { typeof(MsSqlSqlLayerRegistrar.ITestingRegistrar), new MsSqlDbPoolRegistrar(this) },
                              { typeof(MySqlSqlLayerRegistrar.ITestingRegistrar), new MySqlDbPoolRegistrar(this) },
                              { typeof(PgSqlSqlLayerRegistrar.ITestingRegistrar), new PostgreSqlSqlDbPoolRegistrar(this) },
                              { typeof(SqliteSqlLayerRegistrar.ITestingRegistrar), new SqliteSqlDbPoolRegistrar(this) },
                              { typeof(SqliteMemorySqlLayerRegistrar.ITestingRegistrar), new SqliteMemoryDbPoolRegistrar(this) },
                           };
   }

   public override TTestingRegistrar? TryGetTestingRegistrar<TTestingRegistrar>() where TTestingRegistrar : class
   {
      if(_testingRegistrars.TryGetValue(typeof(TTestingRegistrar), out var value))
      {
         return (TTestingRegistrar)value;
      }

      return null;
   }

   public override IRunMode RunMode => Utilities.DependencyInjection.RunMode.Testing;

   public override IComponentRegistrar Clone() => new TestingComponentRegistrar();
}

class MsSqlDbPoolRegistrar(IComponentRegistrar registrar) : MsSqlSqlLayerRegistrar.ITestingRegistrar
{
   readonly IComponentRegistrar _registrar = registrar;
   public IComponentRegistrar Register(string connectionStringName) => _registrar.MicrosoftSqlDbPoolAndConnectionPoolForConnectionStringName(connectionStringName);
}

class MySqlDbPoolRegistrar(IComponentRegistrar registrar) : MySqlSqlLayerRegistrar.ITestingRegistrar
{
   readonly IComponentRegistrar _registrar = registrar;
   public IComponentRegistrar Register(string connectionStringName) => _registrar.MySqlDbPoolWithConnectionPoolForConnectionStringName(connectionStringName);
}

class PostgreSqlSqlDbPoolRegistrar(IComponentRegistrar registrar) : PgSqlSqlLayerRegistrar.ITestingRegistrar
{
   readonly IComponentRegistrar _registrar = registrar;
   public IComponentRegistrar Register(string connectionStringName) => _registrar.PgSqlDbPoolWithConnectionPoolIfNotAlreadyRegistered(connectionStringName);
}

class SqliteSqlDbPoolRegistrar(IComponentRegistrar registrar) : SqliteSqlLayerRegistrar.ITestingRegistrar
{
   readonly IComponentRegistrar _registrar = registrar;
   public IComponentRegistrar Register(string connectionStringName) => _registrar.SqliteDbPoolAndConnectionPoolForConnectionStringNameIfNotAlreadyRegistered(connectionStringName);
}

class SqliteMemoryDbPoolRegistrar(IComponentRegistrar registrar) : SqliteMemorySqlLayerRegistrar.ITestingRegistrar
{
   readonly IComponentRegistrar _registrar = registrar;
   public IComponentRegistrar Register(string connectionStringName) => _registrar.SqliteMemoryDbPoolAndConnectionPoolForConnectionStringNameIfNotAlreadyRegistered(connectionStringName);
}
