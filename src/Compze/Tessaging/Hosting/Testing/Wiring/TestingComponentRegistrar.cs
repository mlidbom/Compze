using System;
using System.Collections.Generic;
using Compze.Abstractions.Internal.Time;
using Compze.Sql.MicrosoftSql;
using Compze.Sql.MySql.SystemExtensions;
using Compze.Sql.PostgreSql;
using Compze.Sql.Sqlite;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Testing.DbPool.MicrosoftSql;
using Compze.Utilities.Testing.DbPool.MySql;
using Compze.Utilities.Testing.DbPool.PostgreSql;
using Compze.Utilities.Testing.DbPool.Sqlite;

namespace Compze.Tessaging.Hosting.Testing.Wiring;

class TestingComponentRegistrar : ComponentRegistrar
{
   readonly IDictionary<Type, object> _testingRegistrars;

   public TestingComponentRegistrar()
   {
      _testingRegistrars = new Dictionary<Type, object>()
                           {
                              { typeof(MsSqlSqlConnectionPoolRegistrar.ITestingRegistrar), new MsSqlDbPoolRegistrar(this) },
                              { typeof(MySqlConnectionPoolRegistrar.ITestingRegistrar), new MySqlDbPoolRegistrar(this) },
                              { typeof(PgSqlConnectionPoolRegistrar.ITestingRegistrar), new PostgreSqlSqlDbPoolRegistrar(this) },
                              { typeof(SqliteConnectionPoolRegistrar.ITestingRegistrar), new SqliteSqlDbPoolRegistrar(this) },
                              { typeof(SqliteMemoryConnectionPoolRegistrar.ITestingRegistrar), new SqliteMemoryDbPoolRegistrar(this) },
                              { typeof(TimeSourceRegistrar.ITestingRegistrar), new TestingTimeSourceRegistrar(this) }
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

   public override IComponentRegistrar Clone() => new TestingComponentRegistrar();

   class TestingTimeSourceRegistrar(IComponentRegistrar registrar) : TimeSourceRegistrar.ITestingRegistrar
   {
      readonly IComponentRegistrar _registrar = registrar;

      public IComponentRegistrar Register() => _registrar.TestingTimeSource();
   }

   class MsSqlDbPoolRegistrar(IComponentRegistrar registrar) : MsSqlSqlConnectionPoolRegistrar.ITestingRegistrar
   {
      readonly IComponentRegistrar _registrar = registrar;
      public IComponentRegistrar Register(string connectionStringName) => _registrar.MicrosoftSqlDbPoolAndConnectionPoolForConnectionStringName(connectionStringName);
   }

   class MySqlDbPoolRegistrar(IComponentRegistrar registrar) : MySqlConnectionPoolRegistrar.ITestingRegistrar
   {
      readonly IComponentRegistrar _registrar = registrar;
      public IComponentRegistrar Register(string connectionStringName) => _registrar.MySqlDbPoolWithConnectionPoolForConnectionStringName(connectionStringName);
   }

   class PostgreSqlSqlDbPoolRegistrar(IComponentRegistrar registrar) : PgSqlConnectionPoolRegistrar.ITestingRegistrar
   {
      readonly IComponentRegistrar _registrar = registrar;
      public IComponentRegistrar Register(string connectionStringName) => _registrar.PgSqlDbPoolWithConnectionPoolIfNotAlreadyRegistered(connectionStringName);
   }

   class SqliteSqlDbPoolRegistrar(IComponentRegistrar registrar) : SqliteConnectionPoolRegistrar.ITestingRegistrar
   {
      readonly IComponentRegistrar _registrar = registrar;
      public IComponentRegistrar Register(string connectionStringName) => _registrar.SqliteDbPoolAndConnectionPoolForConnectionStringNameIfNotAlreadyRegistered(connectionStringName);
   }

   class SqliteMemoryDbPoolRegistrar(IComponentRegistrar registrar) : SqliteMemoryConnectionPoolRegistrar.ITestingRegistrar
   {
      readonly IComponentRegistrar _registrar = registrar;
      public IComponentRegistrar Register(string connectionStringName) => _registrar.SqliteMemoryDbPoolAndConnectionPoolForConnectionStringNameIfNotAlreadyRegistered(connectionStringName);
   }
}
