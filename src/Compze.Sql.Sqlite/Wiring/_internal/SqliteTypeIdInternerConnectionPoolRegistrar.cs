using Compze.Abstractions.Configuration;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Sql.Sqlite._internal;

namespace Compze.Sql.Sqlite.Wiring._internal;

/// <summary>
/// Registers the <see cref="ISqliteTypeIdInternerConnectionPool"/> — the pool for the interner's own database.
/// Mirrors <see cref="SqliteConnectionPoolRegistrar"/>: in a test container it defers to the test database pool,
/// otherwise it reads the connection string from configuration.
/// </summary>
static class SqliteTypeIdInternerConnectionPoolRegistrar
{
   /// <summary>Implemented by the testing infrastructure to point the interner pool at a pooled test database instead of configuration.</summary>
   public interface ITestingRegistrar
   {
      public IComponentRegistrar Register(string connectionStringName);
   }

   public static IComponentRegistrar SqliteTypeIdInternerConnectionPool(this IComponentRegistrar registrar, string connectionStringName)
   {
      if(registrar.TryGetTestingRegistrar<ITestingRegistrar>() is {} testingRegistrar)
         return testingRegistrar.Register(connectionStringName);

      return registrar.Register(
         Singleton.For<ISqliteTypeIdInternerConnectionPool>()
                  .CreatedBy((IConfigurationParameterProvider configurationParameterProvider) =>
                                new ISqliteTypeIdInternerConnectionPool.Pool(() => configurationParameterProvider.GetString(connectionStringName))));
   }
}
