using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Sql.MySql.Wiring;

static class MySqlSchemaContributionRegistrar
{
   ///<summary>Contributes <paramref name="schemaCreationSql"/> — one feature backend's <see cref="Private.MySqlSchemaContribution"/> —<br/>
   /// to the schema of the database behind the endpoint's <see cref="IMySqlConnectionPool"/>, and on the first contribution registers the<br/>
   /// <see cref="Private.MySqlSqlLayerSchemaManager"/> that creates every contributed schema in a single batch before the database's first use.<br/>
   /// Called by each MySQL feature backend's registration — never by a composing layer, which stays ignorant of schemas entirely.</summary>
   internal static IComponentRegistrar MySqlSchemaContribution(this IComponentRegistrar registrar, string schemaCreationSql)
   {
      registrar.Register(Singleton.ForSet<Private.MySqlSchemaContribution>()
                                  .CreatedBy(() => new Private.MySqlSchemaContribution(schemaCreationSql)));

      if(!registrar.IsRegistered<Private.MySqlSqlLayerSchemaManager>())
      {
         registrar.Register(Singleton.For<Private.MySqlSqlLayerSchemaManager>()
                                     .DelegateToParentServiceLocatorWhenCloning()
                                     .CreatedBy((IMySqlConnectionPool connectionPool, IComponentSet<Private.MySqlSchemaContribution> contributions)
                                                   => new Private.MySqlSqlLayerSchemaManager(connectionPool, [..contributions.Select(it => it.SchemaCreationSql)])));
      }

      return registrar;
   }
}
