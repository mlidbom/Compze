using Compze.Persistence.MicrosoftSql.DependencyInjection;
using Compze.Tessaging.Buses;

namespace AccountManagement;

public static class AccountManagementPersistenceLayerRegistrar
{
   public static void RegisterPersistenceLayer(this IEndpointBuilder builder)
   {
      // Production default: Use Microsoft SQL Server
      // In a real application, this could be determined from:
      // - Environment variables
      // - Configuration files read at startup
      // - Command-line arguments
      // - Cloud configuration services
      // For this sample, we default to MsSql as specified in appsettings.json
      
      builder.Container.RegisterMsSqlPersistenceLayer(builder.Configuration.ConnectionStringName);
      
      // Alternative implementations would require adding their respective project references:
      // builder.Container.RegisterMySqlPersistenceLayer(builder.Configuration.ConnectionStringName);
      // builder.Container.RegisterPgSqlPersistenceLayer(builder.Configuration.ConnectionStringName);
   }
}
