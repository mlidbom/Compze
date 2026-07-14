using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Sql.MicrosoftSql.Wiring;

static class MsSqlSchemaContributionRegistrar
{
   ///<summary>Contributes <paramref name="schemaCreationSql"/> — one feature backend's <see cref="Private.MsSqlSchemaContribution"/> —<br/>
   /// to the schema of the database behind the endpoint's <see cref="IMsSqlConnectionPool"/>, and on the first contribution registers the<br/>
   /// <see cref="Private.MsSqlSqlLayerSchemaManager"/> that creates every contributed schema in a single batch before the database's first use.<br/>
   /// Called by each MS SQL feature backend's registration — never by a composing layer, which stays ignorant of schemas entirely.</summary>
   internal static IComponentRegistrar MsSqlSchemaContribution(this IComponentRegistrar registrar, string schemaCreationSql)
   {
      registrar.Register(Singleton.ForSet<Private.MsSqlSchemaContribution>()
                                  .CreatedBy(() => new Private.MsSqlSchemaContribution(schemaCreationSql)));

      if(!registrar.IsRegistered<Private.MsSqlSqlLayerSchemaManager>())
      {
         registrar.Register(Singleton.For<Private.MsSqlSqlLayerSchemaManager>()
                                     .DelegateToParentServiceLocatorWhenCloning()
                                     .CreatedBy((IMsSqlConnectionPool connectionPool, IComponentSet<Private.MsSqlSchemaContribution> contributions)
                                                   => new Private.MsSqlSqlLayerSchemaManager(connectionPool, [..contributions.Select(it => it.SchemaCreationSql)])));
      }

      return registrar;
   }
}
