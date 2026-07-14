using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Sql.Sqlite.Wiring;

static class SqliteSchemaContributionRegistrar
{
   ///<summary>Contributes <paramref name="schemaCreationSql"/> — one feature backend's <see cref="Private.SqliteSchemaContribution"/> —<br/>
   /// to the schema of the database behind the endpoint's <see cref="ISqliteConnectionPool"/>, and on the first contribution registers the<br/>
   /// <see cref="Private.SqliteSqlLayerSchemaManager"/> that creates every contributed schema in a single batch before the database's first use.<br/>
   /// Called by each sqlite feature backend's registration — never by a composing layer, which stays ignorant of schemas entirely.</summary>
   internal static IComponentRegistrar SqliteSchemaContribution(this IComponentRegistrar registrar, string schemaCreationSql)
   {
      registrar.Register(Singleton.ForSet<Private.SqliteSchemaContribution>()
                                  .CreatedBy(() => new Private.SqliteSchemaContribution(schemaCreationSql)));

      if(!registrar.IsRegistered<Private.SqliteSqlLayerSchemaManager>())
      {
         registrar.Register(Singleton.For<Private.SqliteSqlLayerSchemaManager>()
                                     .DelegateToParentServiceLocatorWhenCloning()
                                     .CreatedBy((ISqliteConnectionPool connectionPool, IComponentSet<Private.SqliteSchemaContribution> contributions)
                                                   => new Private.SqliteSqlLayerSchemaManager(connectionPool, [..contributions.Select(it => it.SchemaCreationSql)])));
      }

      return registrar;
   }
}
