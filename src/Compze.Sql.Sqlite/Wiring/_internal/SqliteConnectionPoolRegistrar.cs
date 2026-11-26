using Compze.Abstractions.Configuration;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Sql.Sqlite._internal;
using Microsoft.Data.Sqlite;

namespace Compze.Sql.Sqlite.Wiring._internal;

static class SqliteConnectionPoolRegistrar
{
   public interface ITestingRegistrar
   {
      public IComponentRegistrar Register(string connectionStringName);
   }

   public static IComponentRegistrar SqliteConnectionPool(this IComponentRegistrar registrar, string connectionStringName)
   {
      if(registrar.TryGetTestingRegistrar<ITestingRegistrar>() is {} testingRegistrar)
      {
         return testingRegistrar.Register(connectionStringName);
      } else
      {
         registrar.SqliteProductionConnectionPool(connectionStringName);
      }

      return registrar;
   }

   ///<summary>Registers the pool of the database in <paramref name="databaseFile"/>, in a testing container too.</summary>
   public static IComponentRegistrar SqliteConnectionPool(this IComponentRegistrar registrar, FileInfo databaseFile) =>
      registrar.Register(
         Singleton.For<ISqliteConnectionPool>()
                  .CreatedBy(() => ISqliteConnectionPool.CreateInstance(new SqliteConnectionStringBuilder { DataSource = databaseFile.FullName }.ConnectionString)));

   // ReSharper disable once UnusedMethodReturnValue.Local
   static IComponentRegistrar SqliteProductionConnectionPool(this IComponentRegistrar registrar, string connectionStringName)
      => registrar.Register(
         Singleton.For<ISqliteConnectionPool>()
                  .CreatedBy((IConfigurationParameterProvider configurationParameterProvider) => ISqliteConnectionPool.CreateInstance(configurationParameterProvider.GetString(connectionStringName))));
}
