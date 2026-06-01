using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.MicrosoftSql;
using Compze.Internals.Sql.MicrosoftSql.Private;

namespace Compze.TypeIdentifiers.Interning.MicrosoftSql.Wiring;

public static class MsSqlTypeIdInternerRegistrar
{
   public static string SchemaCreationSql => MsSqlTypeIdInternerPersistence.SchemaCreationSql;

   public static IComponentRegistrar MsSqlTypeIdInterner(this IComponentRegistrar registrar)
   {
      if(registrar.IsRegistered<ITypeIdInterner>())
         return registrar;

      return registrar.Register(
         Singleton.For<ITypeIdInternerPersistence>()
                  .CreatedBy((IMsSqlConnectionPool connectionPool, MsSqlSqlLayerSchemaManager schemaManager) => new MsSqlTypeIdInternerPersistence(connectionPool, schemaManager)),
         Singleton.For<ITypeIdInterner>()
                  .CreatedBy((ITypeIdInternerPersistence persistence, ITypeMap typeMap) => TypeIdInterner.For(persistence, typeMap)));
   }
}
