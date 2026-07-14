using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Sql.PostgreSql.Wiring;

static class PgSqlSchemaContributionRegistrar
{
   ///<summary>Contributes <paramref name="schemaCreationSql"/> — one feature backend's <see cref="Private.PgSqlSchemaContribution"/> —<br/>
   /// to the schema of the database behind the endpoint's <see cref="IPgSqlConnectionPool"/>, and on the first contribution registers the<br/>
   /// <see cref="Private.PgSqlSqlLayerSchemaManager"/> that creates every contributed schema in a single batch before the database's first use.<br/>
   /// Called by each PostgreSQL feature backend's registration — never by a composing layer, which stays ignorant of schemas entirely.</summary>
   internal static IComponentRegistrar PgSqlSchemaContribution(this IComponentRegistrar registrar, string schemaCreationSql)
   {
      registrar.Register(Singleton.ForSet<Private.PgSqlSchemaContribution>()
                                  .CreatedBy(() => new Private.PgSqlSchemaContribution(schemaCreationSql)));

      if(!registrar.IsRegistered<Private.PgSqlSqlLayerSchemaManager>())
      {
         registrar.Register(Singleton.For<Private.PgSqlSqlLayerSchemaManager>()
                                     .DelegateToParentServiceLocatorWhenCloning()
                                     .CreatedBy((IPgSqlConnectionPool connectionPool, IComponentSet<Private.PgSqlSchemaContribution> contributions)
                                                   => new Private.PgSqlSqlLayerSchemaManager(connectionPool, [..contributions.Select(it => it.SchemaCreationSql)])));
      }

      return registrar;
   }
}
