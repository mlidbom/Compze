using System;
using System.Collections.Generic;
using Compze.Core.Time.Public;
using Compze.Core.Time.Testing.Public;
using Compze.Core.Wiring.Testing.Internal;
using Compze.Serialization.Newtonsoft.Wiring;
using Compze.Sql.MicrosoftSql;
using Compze.Sql.MicrosoftSql.DbPool;
using Compze.Sql.MicrosoftSql.DocumentDb.Wiring;
using Compze.Sql.MicrosoftSql.Tessaging;
using Compze.Sql.MicrosoftSql.TEventStore;
using Compze.Sql.MySql.DbPool;
using Compze.Sql.MySql.DocumentDb.Wiring;
using Compze.Sql.MySql.SystemExtensions;
using Compze.Sql.MySql.Tessaging;
using Compze.Sql.MySql.TEventStore;
using Compze.Sql.PostgreSql;
using Compze.Sql.PostgreSql.DbPool;
using Compze.Sql.PostgreSql.DocumentDb.Wiring;
using Compze.Sql.PostgreSql.Tessaging;
using Compze.Sql.PostgreSql.TEventStore;
using Compze.Sql.Sqlite;
using Compze.Sql.Sqlite.DbPool;
using Compze.Sql.Sqlite.DocumentDb.Wiring;
using Compze.Sql.Sqlite.Tessaging;
using Compze.Sql.Sqlite.TEventStore;
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

   public IComponentRegistrar CurrentTestsDbPoolIfNotAlreadyRegistered()
   {
      this.DbPoolIfNotAlreadyRegistered();
      switch(TestEnv.SqlLayer)
      {
         case SqlLayer.MicrosoftSqlServer:
            return this.MsSqlDbPoolSqlLayerIfNotAlreadyRegistered();
         case SqlLayer.MySql:
            return this.MySqlDbPoolSqlLayerIfNotAlreadyRegistered();
         case SqlLayer.PostgreSql:
            return this.PgSqlDbPoolSqlLayerIfNotAlreadyRegistered();
         case SqlLayer.Sqlite:
            return this.SqliteDbPoolSqlLayerIfNotAlreadyRegistered();
         case SqlLayer.SqliteMemory:
            return  this.SqliteMemoryDbPoolSqlLayerIfNotAlreadyRegistered();
         default:
            throw new ArgumentOutOfRangeException();
      }
   }

   public IComponentRegistrar CurrentTestsSerializers()
   {
      return this.NewtonsoftSerializers();
   }

   public IComponentRegistrar CurrentTestsConfiguredSqlLayer(string connectionStringName)
   {
      switch(TestEnv.SqlLayer)
      {
         case SqlLayer.MicrosoftSqlServer:
            this.MsSqlConnectionPool(connectionStringName)
                    .MsSqlDocumentDb()
                    .MsSqlTeventStore()
                    .MsSqlTessaging();
            break;
         case SqlLayer.MySql:
            this.MySqlConnectionPool(connectionStringName)
                .MySqlDocumentDb()
                .MySqlTeventStore()
                .MySqlTessaging();
            break;
         case SqlLayer.PostgreSql:
            this.PgSqlConnectionPoolIfNotAlreadyRegistered(connectionStringName)
                .PgSqlDocumentDb()
                .PgSqlTeventStore()
                .PgSqlTessaging();
            break;
         case SqlLayer.Sqlite:
            this.SqliteConnectionPool(connectionStringName)
                .SqliteDocumentDb()
                .SqliteTeventStore()
                .SqliteTessaging();
            break;
         case SqlLayer.SqliteMemory:
            this.SqliteMemoryConnectionPool(connectionStringName)
                .SqliteDocumentDb()
                .SqliteTeventStore()
                .SqliteTessaging();
            break;
         default:
            throw new ArgumentOutOfRangeException();
      }

      return this;
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

      public IComponentRegistrar Register(string connectionStringName) => _registrar.DbPoolIfNotAlreadyRegistered()
                                                                                    .MsSqlDbPoolSqlLayerIfNotAlreadyRegistered()
                                                                                    .Register(
                                                                                        Singleton.For<IMsSqlConnectionPool>()
                                                                                                 .CreatedBy((DbPool dbPool) =>
                                                                                                               IMsSqlConnectionPool.CreateInstance(() => dbPool.ConnectionStringFor(connectionStringName))));
   }

   class MySqlDbPoolRegistrar(IComponentRegistrar registrar) : MySqlConnectionPoolRegistrar.ITestingRegistrar
   {
      readonly IComponentRegistrar _registrar = registrar;

      public IComponentRegistrar Register(string connectionStringName) => _registrar.DbPoolIfNotAlreadyRegistered()
                                                                                    .MySqlDbPoolSqlLayerIfNotAlreadyRegistered()
                                                                                    .Register(
                                                                                        Singleton.For<IMySqlConnectionPool>()
                                                                                                 .CreatedBy((DbPool dbPool) =>
                                                                                                               IMySqlConnectionPool.CreateInstance(() => dbPool.ConnectionStringFor(connectionStringName))));
   }

   class PostgreSqlSqlDbPoolRegistrar(IComponentRegistrar registrar) : PgSqlConnectionPoolRegistrar.ITestingRegistrar
   {
      readonly IComponentRegistrar _registrar = registrar;

      public IComponentRegistrar Register(string connectionStringName) => _registrar.DbPoolIfNotAlreadyRegistered()
                                                                                    .PgSqlDbPoolSqlLayerIfNotAlreadyRegistered()
                                                                                    .Register(
                                                                                        Singleton.For<IPgSqlConnectionPool>()
                                                                                                 .CreatedBy((DbPool pool) => IPgSqlConnectionPool.CreateInstance1(() => pool.ConnectionStringFor(connectionStringName))));
   }

   class SqliteSqlDbPoolRegistrar(IComponentRegistrar registrar) : SqliteConnectionPoolRegistrar.ITestingRegistrar
   {
      readonly IComponentRegistrar _registrar = registrar;

      public IComponentRegistrar Register(string connectionStringName) => _registrar.DbPoolIfNotAlreadyRegistered()
                                                                                    .SqliteDbPoolSqlLayerIfNotAlreadyRegistered()
                                                                                    .Register(
                                                                                        Singleton.For<ISqliteConnectionPool>()
                                                                                                 .CreatedBy((DbPool pool) => ISqliteConnectionPool.CreateInstance(() => pool.ConnectionStringFor(connectionStringName))));
   }

   class SqliteMemoryDbPoolRegistrar(IComponentRegistrar registrar) : SqliteMemoryConnectionPoolRegistrar.ITestingRegistrar
   {
      readonly IComponentRegistrar _registrar = registrar;

      public IComponentRegistrar Register(string connectionStringName) => _registrar.DbPoolIfNotAlreadyRegistered()
                                                                                    .SqliteMemoryDbPoolSqlLayerIfNotAlreadyRegistered()
                                                                                    .Register(
                                                                                        Singleton.For<ISqliteConnectionPool>()
                                                                                                 .CreatedBy((DbPool pool) => ISqliteConnectionPool.CreateInstance(() => pool.ConnectionStringFor(connectionStringName))));
   }
}
