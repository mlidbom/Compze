using Compze.Sql.MicrosoftSql;
using Compze.Sql.MicrosoftSql.Wiring;
using Compze.Sql.MySql;
using Compze.Sql.MySql.Wiring;
using Compze.Sql.PostgreSql;
using Compze.Sql.PostgreSql.Wiring;
using Compze.Sql.Sqlite;
using Compze.Sql.Sqlite.Wiring;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Testing.DbPool;

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
                              { typeof(SqliteMemoryConnectionPoolRegistrar.ITestingRegistrar), new SqliteMemoryDbPoolRegistrar(this) }
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

   class MsSqlDbPoolRegistrar(IComponentRegistrar registrar) : MsSqlSqlConnectionPoolRegistrar.ITestingRegistrar
   {
      readonly IComponentRegistrar _registrar = registrar;

      public IComponentRegistrar Register(string connectionStringName) => _registrar.CurrentTestsDbPoolIfNotCloneContainer()
                                                                                    .Register(
                                                                                        Singleton.For<IMsSqlConnectionPool>()
                                                                                                 .CreatedBy((DbPool dbPool) =>
                                                                                                               IMsSqlConnectionPool.CreateInstance(() => dbPool.ConnectionStringFor(connectionStringName))));
   }

   class MySqlDbPoolRegistrar(IComponentRegistrar registrar) : MySqlConnectionPoolRegistrar.ITestingRegistrar
   {
      readonly IComponentRegistrar _registrar = registrar;

      public IComponentRegistrar Register(string connectionStringName) => _registrar.CurrentTestsDbPoolIfNotCloneContainer()
                                                                                    .Register(
                                                                                        Singleton.For<IMySqlConnectionPool>()
                                                                                                 .CreatedBy((DbPool dbPool) =>
                                                                                                               IMySqlConnectionPool.CreateInstance(() => dbPool.ConnectionStringFor(connectionStringName))));
   }

   class PostgreSqlSqlDbPoolRegistrar(IComponentRegistrar registrar) : PgSqlConnectionPoolRegistrar.ITestingRegistrar
   {
      readonly IComponentRegistrar _registrar = registrar;

      public IComponentRegistrar Register(string connectionStringName) => _registrar.CurrentTestsDbPoolIfNotCloneContainer()
                                                                                    .Register(
                                                                                        Singleton.For<IPgSqlConnectionPool>()
                                                                                                 .CreatedBy((DbPool pool) => IPgSqlConnectionPool.CreateInstance1(() => pool.ConnectionStringFor(connectionStringName))));
   }

   class SqliteSqlDbPoolRegistrar(IComponentRegistrar registrar) : SqliteConnectionPoolRegistrar.ITestingRegistrar
   {
      readonly IComponentRegistrar _registrar = registrar;

      public IComponentRegistrar Register(string connectionStringName) => _registrar.CurrentTestsDbPoolIfNotCloneContainer()
                                                                                    .Register(
                                                                                        Singleton.For<ISqliteConnectionPool>()
                                                                                                 .CreatedBy((DbPool pool) => ISqliteConnectionPool.CreateInstance(() => pool.ConnectionStringFor(connectionStringName))));
   }

   class SqliteMemoryDbPoolRegistrar(IComponentRegistrar registrar) : SqliteMemoryConnectionPoolRegistrar.ITestingRegistrar
   {
      readonly IComponentRegistrar _registrar = registrar;

      public IComponentRegistrar Register(string connectionStringName) => _registrar.CurrentTestsDbPoolIfNotCloneContainer()
                                                                                    .Register(
                                                                                        Singleton.For<ISqliteConnectionPool>()
                                                                                                 .CreatedBy((DbPool pool) => ISqliteConnectionPool.CreateInstance(() => pool.ConnectionStringFor(connectionStringName))));
   }
}
